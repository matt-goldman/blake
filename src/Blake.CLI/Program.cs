using Blake.BuildTools.Generator;
using System.Diagnostics;
using Blake.BuildTools.Services;
using Blake.Types;
using Microsoft.Extensions.Logging;

namespace Blake.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var cts = new CancellationTokenSource();

        // Handle Ctrl+C gracefully
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true; // Don't terminate immediately
            cts.Cancel(); // Signal cancellation
        };

        var logger = CreateLoggerFactory(args).CreateLogger<Program>();

        return await RunAsync(args, logger, cts.Token);
    }

    internal static async Task<int> RunAsync(string[] args, ILogger logger, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
        {
            await Console.Error.WriteLineAsync("Required argument missing.");
            ShowHelp();
            return 1;
        }

        var option = args[0].ToLowerInvariant();

        switch (option)
        {
            case "--help":
                ShowHelp();
                return 0;
            case "init":
                return await InitBlakeAsync(args, logger, cancellationToken);
            case "bake":
                return await BakeBlakeAsync(args, logger, cancellationToken);
            case "serve":
                return await ServeBakeAsync(args, logger, cancellationToken);
            case "new":
                return await NewSiteAsync(args, logger, cancellationToken);
            default:
                logger.LogError("Unknown option: {option}", option);
                return 1;
        }
    }

    private static ILoggerFactory CreateLoggerFactory(string[] args)
    {
        var level = ParseLevel(args);
        return LoggerFactory.Create(c =>
        {
            c.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.None); // force stdout
            c.SetMinimumLevel(level);
        });
    }

    private static LogLevel ParseLevel(string[] args)
    {
        var level = LogLevel.Warning;
        var i = Array.FindIndex(args, a => a is "--verbosity" or "-v");
        if (i >= 0 && i + 1 < args.Length)
            _ = Enum.TryParse(args[i + 1], ignoreCase: true, out level);
        return level;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  blake <command> [path] [options]");
        Console.WriteLine();
        Console.WriteLine("If no [path] is provided, the current working directory is used.");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  init <PATH>          Configure an existing Blazor WASM app for Blake.");
        Console.WriteLine("                       Options:");
        Console.WriteLine("                         --includeSampleContent, -s   Include sample page and update nav");
        Console.WriteLine();
        Console.WriteLine("  bake <PATH>          Generate static content for a Blake site.");
        Console.WriteLine("                       Options:");
        Console.WriteLine("                         --disableDefaultRenderers, -dr   Disable the built-in Bootstrap container renderers");
        Console.WriteLine("                         --includeDrafts                  Bakes markdown files that contain 'draft: true' in the frontmatter (they are skipped by default)");
        Console.WriteLine("                         --clean, -cl                     Deletes the .generated folder before re-generating site content");
        Console.WriteLine("                         --continueOnError, -ce           Continues baking even if some pages fail to generate. By default, the process stops on the first error.");
        Console.WriteLine();
        Console.WriteLine("  new <PATH>           Generates a new Blake site");
        Console.WriteLine("                       Options:");
        Console.WriteLine("                         --template, -t   The name or short name of the template to create a site from (optional). Uses the default Blazor WASM template if not specified.");
        Console.WriteLine("                         --siteName, -sn  The name of the new site (optional). Uses the directory name if not specified. If configured in the template, replaces the template name with the provided name.");
        Console.WriteLine("                         --url, -u        The URL of the repo that hosts the template (optional). Can be used to install templates outside the public Blake registry. Uses Git, so should work with any repo you have access to.");
        Console.WriteLine("                         --list           Lists all available templates in the public Blake registry");
        Console.WriteLine();
        Console.WriteLine("  serve <PATH>         Bake and run the Blazor app in development mode.");
        Console.WriteLine("                       Options:");
        Console.WriteLine("                         --disableDefaultRenderers, -dr   Disable the built-in Bootstrap container renderers");
        Console.WriteLine();
        Console.WriteLine("  --help               Show this help message.");
    }
    
    private static async Task<int> InitBlakeAsync(string[] args, ILogger logger, CancellationToken cancellationToken)
    {
        // get target path or use current directory
        var targetPath = GetPathFromArgs(args);

        var projectFile = string.Empty;
        
        // Check if the path is a .csproj file
        if (Path.GetExtension(targetPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(targetPath))
            {
                logger.LogError("Project file '{targetPath}' does not exist.", targetPath);
                return 1;
            }
            projectFile = targetPath;
            targetPath = Path.GetDirectoryName(targetPath) ?? Directory.GetCurrentDirectory();
        }
        else
        {
            // If it's a directory, check for .csproj files
            var csprojFiles = Directory.GetFiles(targetPath, "*.csproj");
            if (csprojFiles.Length == 0)
            {
                logger.LogError("No .csproj file found in the specified path.");
                return 1;
            }
            projectFile = csprojFiles[0];
        }
        
        if (!Directory.Exists(targetPath))
        {
            logger.LogError("Path '{targetPath}' does not exist.", targetPath);
            return 1;
        }
        
        logger.LogInformation("🛠  Initializing Blake in: {targetPath}", targetPath);
        
        // Check if the project is a Blazor WASM app
        if (!File.Exists(projectFile))
        {
            logger.LogError("Project file '{projectFile}' does not exist.", projectFile);
            return 1;
        }

        var includeSampleContent = args.Contains("--includeSampleContent") || args.Contains("-s");

        await SiteGenerator.InitAsync(projectFile, logger, cancellationToken, includeSampleContent);

        logger.LogInformation("✅ Blake initialized successfully in {targetPath}", targetPath);

        Console.WriteLine("Completed init, running Bake...");

        return await BakeBlakeAsync(args, logger, cancellationToken);
    }

    private static async Task<int> BakeBlakeAsync(string[] args, ILogger logger, CancellationToken cancellationToken)
    {
        string targetPath;

        try
        {
            // get target path or use current directory
            targetPath = GetPathFromArgs(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Failed to parse target path from arguments. Please provide a valid path.");
            logger.LogError(ex, "Failed to parse target path from arguments.");
            return 1;
        }

        logger.LogInformation("Baking Blake site in: {targetPath}", targetPath);

        // Build context expects a directory, not a .csproj file
        if (Path.GetExtension(targetPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(targetPath))
            {
                logger.LogError("Project file '{targetPath}' does not exist.", targetPath);
                return 1;
            }
            targetPath = Path.GetDirectoryName(targetPath) ?? Directory.GetCurrentDirectory();
        }

        if (!Directory.Exists(targetPath))
        {
            logger.LogError("Path '{targetPath}' does not exist.", targetPath);
            return 1;
        }

        logger.LogInformation("🛠  Starting build for: {targetPath}", targetPath);

        var options = new GenerationOptions
        {
            ProjectPath         = targetPath,
            OutputPath          = Path.Combine(targetPath, ".generated"),
            UseDefaultRenderers = !args.Contains("--disableDefaultRenderers") && !args.Contains("-dr"),
            IncludeDrafts       = args.Contains("--includeDrafts"),
            Clean               = args.Contains("--clean") || args.Contains("-cl"),
            ContinueOnError     = args.Contains("--continueOnError") || args.Contains("-ce"),
            Arguments           = [.. args.Skip(1)]
        };

        try
        {
            await SiteGenerator.BuildAsync(options, logger, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ An error occurred while baking the site. Please check the logs for details.");
            logger.LogError(ex, "Baking site failed");
        }

        logger.LogInformation("✅ Build completed successfully for: {targetPath}", targetPath);
        Console.WriteLine("✅ Build completed successfully.");
        
        return 0;
    }

    private static async Task<int> ServeBakeAsync(string[] args, ILogger logger, CancellationToken cancellationToken)
    {
        var path = args.Length > 1 ? args[1].Trim('"') : Directory.GetCurrentDirectory();

        Console.WriteLine($"🔧 Baking in: {path}");
        var bakeResult = await BakeBlakeAsync(args, logger, cancellationToken);
        if (bakeResult != 0) return bakeResult;

        Console.WriteLine("🚀 Running app...");

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo("dotnet", $"run --project \"{path}\"")
        {
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false  // Changed for better control
        };

        if (!process.Start())
        {
            logger.LogError("Failed to start the process. Please check the configuration and try again.");
            return 1;
        }

        try
        {
            // Use the passed cancellation token directly
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Serve command cancelled, terminating dotnet run process...");
            TerminateProcessGracefully(process, logger);
        }

        return 0;
    }

    private static void TerminateProcessGracefully(Process process, ILogger logger)
    {
        if (process.HasExited) return;

        logger.LogDebug("Attempting graceful shutdown of dotnet run process...");

        try
        {
            // Try graceful shutdown first
            if (OperatingSystem.IsWindows())
            {
                if (process.CloseMainWindow())
                {
                    if (process.WaitForExit(3000)) // Wait up to 3 seconds
                        return;
                }
            }
            else
            {
                // On Unix, send SIGTERM first
                process.Kill(entireProcessTree: false);
                if (process.WaitForExit(3000)) // Wait up to 3 seconds
                    return;
            }

            // Force kill if graceful shutdown didn't work
            logger.LogDebug("Graceful shutdown failed, force killing process tree...");
            process.Kill(entireProcessTree: true);

        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error during process shutdown");
        }
    }


    private static async Task<int> NewSiteAsync(string[] args, ILogger logger, CancellationToken cancellationToken)
    {
        Console.WriteLine();

        var templateService = new TemplateService();
        
        if (args[1] == "--list")
        {
            var templates = await templateService.GetTemplatesAsync(cancellationToken);
            
            var templateList = templates.OrderBy(t => t.Name).ToList() ?? new List<SiteTemplate>();
            
            // TODO: Improve this with Specter.Console or similar
            if (templateList.Count > 0)
            {
                logger.LogInformation("Available templates: {count}", templateList.Count);
                Console.WriteLine("Available templates:");
                Console.WriteLine("Template Name             | Short name       | Description               | Main Category       | Author");
                Console.WriteLine("--------------------------|------------------|---------------------------|---------------------|-----------------");
                foreach (var template in templateList)
                {
                    Console.WriteLine($"{template.Name,-26} | {template.ShortName,-16} | {template.Description,-25} | {template.MainCategory,-19} | {template.Author}");
                    logger.LogInformation("Template: {name} ({shortName}) - {description} | Category: {category} | Author: {author}", 
                        template.Name, template.ShortName, template.Description, template.MainCategory, template.Author);
                }
            }
            else
            {
                Console.WriteLine("No templates found.");
                logger.LogDebug("No templates found.");
            }
            
            return 0;
        }
        
        var argList = args.ToList();
        var newSiteName = string.Empty;
        var templateName = string.Empty;
        var directory = GetPathFromArgs(args);

        var result = 0;

        logger.LogInformation("🛠  Creating new site in: {directory}", directory);

        if (argList.Contains("--siteName") || argList.Contains("-sn"))
        {
            var siteNameFlagIndex = argList.FindIndex(arg => arg is "--siteName" or "-sn");

            if (siteNameFlagIndex >= 0)
            {
                newSiteName = argList[siteNameFlagIndex + 1];

                if (string.IsNullOrWhiteSpace(newSiteName) || newSiteName.Contains(Path.DirectorySeparatorChar) || newSiteName.Contains(Path.AltDirectorySeparatorChar))
                {
                    logger.LogError("Site name '{newSiteName}' is invalid. It should not contain directory separators.", newSiteName);
                    return 1;
                }

                logger.LogInformation("Using provided site name: {newSiteName}", newSiteName);
            }
        }
        else
        {
            var directoryParts = directory.Split(Path.DirectorySeparatorChar);
            newSiteName = directoryParts[^1];
            logger.LogInformation("No site name provided, using directory name: {newSiteName}", newSiteName);
        }
        

        if (argList.Contains("--url") || argList.Contains("-u"))
        {
            var urlFlagArgIndex = argList.FindIndex(arg => arg.StartsWith("--url") || arg.StartsWith("-u"));
            var url = argList[urlFlagArgIndex + 1];

            if (string.IsNullOrWhiteSpace(url))
            {
                logger.LogError("Url '{url}' is missing or invalid.", url);
                return 1;
            }

            result = await templateService.CloneTemplateAsync(newSiteName, logger, cancellationToken, directory, repoUrl: url);
        }
        else
        {
            var templateFlagArg = argList.FindIndex(arg => arg.StartsWith("--template") || arg.StartsWith("-t"));

            if (templateFlagArg > 0)
            {
                // get the template name
                templateName = argList[templateFlagArg + 1];
                result = await templateService.CloneTemplateAsync(templateName, logger, cancellationToken, directory);
            }
            else
            {
                // don't use a template, just create a new Blazor site and initialise
                logger.LogInformation("No template specified, creating a new Blazor WASM site using the default template.");

                var nameFlag = string.IsNullOrWhiteSpace(newSiteName)? string.Empty : $" --name \"{newSiteName}\"";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo("dotnet", $"new blazorwasm -o \"{directory}\"{nameFlag}")
                    {
                        RedirectStandardOutput  = true,
                        RedirectStandardError   = true,
                        UseShellExecute         = false,
                        CreateNoWindow          = true
                    }
                };
                
                var processResult = process.Start();
                
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode == 0)
                {
                    var fileName = Path.Combine(directory, $"{newSiteName}.csproj");
                    var initResult = await SiteGenerator.InitAsync(fileName, logger, cancellationToken, true);

                    if (initResult == 0)
                    {
                        logger.LogInformation("✅ New site {newSiteName} created successfully.", newSiteName);
                        return await BakeBlakeAsync([string.Empty, fileName], logger, cancellationToken); // ensure path is second argument
                    }
                    else
                    {
                        logger.LogError("Failed to create new Blazor WASM site. Error: {error}", process.StandardError.ReadToEnd());
                    }

                    return initResult;
                }
            }
        }

        if (result != 0)
        {
            logger.LogError("Failed to create site from template.");
            return result;
        }

        var newResult = await SiteGenerator.NewSiteAsync(newSiteName, templateName, directory, logger, cancellationToken);

        if (newResult != 0)
        {
            logger.LogError("Failed to create new Blake site");
            return newResult;
        }
        else
        {
            logger.LogInformation("✅ New site {newSiteName} created successfully.", newSiteName);
        }

        return await BakeBlakeAsync(args, logger, cancellationToken);
    }

    private static string GetPathFromArgs(string[] args)
    {
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]) && !args[1].StartsWith('-'))
        {
            var suppliedDirectory = args[1];

            // remove trailing slashes or quotes
            suppliedDirectory = suppliedDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '"', '\'');

            return suppliedDirectory;
        }

        return Directory.GetCurrentDirectory();
    }
}
