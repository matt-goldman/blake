using Blake.BuildTools.Generator;
using System.Diagnostics;
using Blake.BuildTools.Services;
using Blake.Types;
using Microsoft.Extensions.Logging;

namespace Blake.CLI;

class Program
{
    static async Task<int> Main(string[] args)
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
                return await InitBlakeAsync(args);
            case "bake":
                return await BakeBlakeAsync(args);
            case "serve":
                return await ServeBakeAsync(args);
            case "new":
                return await NewSiteAsync(args);
            default:
                await Console.Error.WriteLineAsync($"Unknown option: {option}");
        
                return 1;
        }
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
    
    private static async Task<int> InitBlakeAsync(string[] args)
    {
        // get target path or use current directory
        var targetPath = GetPathFromArgs(args);

        var projectFile = string.Empty;

        var logger = GetLogger(args);
        
        // Check if the path is a .csproj file
        if (Path.GetExtension(targetPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(targetPath))
            {
                logger?.LogError("Project file '{targetPath}' does not exist.", targetPath);
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

        return await SiteGenerator.InitAsync(projectFile, includeSampleContent, logger);
    }

    private static async Task<int> BakeBlakeAsync(string[] args)
    {
        // get target path or use current directory
        var targetPath = GetPathFromArgs(args);

        var logger = GetLogger(args);

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
            Arguments           = [.. args.Skip(1)]
        };

        await SiteGenerator.BuildAsync(options, logger);

        Console.WriteLine("✅ Build completed successfully.");
        
        return 0;
    }

    private static async Task<int> ServeBakeAsync(string[] args)
    {
        var path = args.Length > 1 ? args[1].Trim('"') : Directory.GetCurrentDirectory();

        Console.WriteLine($"🔧 Baking in: {path}");
        var bakeResult = await BakeBlakeAsync(args);
        if (bakeResult != 0) return bakeResult;

        Console.WriteLine("🚀 Running app...");
        var psi = new ProcessStartInfo("dotnet", $"run --project \"{path}\"")
        {
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = true
        };

        var process = Process.Start(psi);
        if (process == null)
        {
            Console.WriteLine("❌ Failed to start the process. Please check the configuration and try again.");
            return 1;
        }
        await process.WaitForExitAsync();
        return 0;
    }

    private static async Task<int> NewSiteAsync(string[] args)
    {
        Console.WriteLine();

        var templateService = new TemplateService();
        
        if (args[1] == "--list")
        {
            var templates = await templateService.GetTemplatesAsync();
            
            var templateList = templates.OrderBy(t => t.Name).ToList() ?? new List<SiteTemplate>();
            
            // TODO: Improve this with Specter.Console or similar
            if (templateList.Count > 0)
            {
                Console.WriteLine("Available templates:");
                Console.WriteLine("Template Name             | Short name       | Description               | Main Category       | Author");
                Console.WriteLine("--------------------------|------------------|---------------------------|---------------------|-----------------");
                foreach (var template in templateList)
                {
                    Console.WriteLine($"{template.Name,-26} | {template.ShortName,-16} | {template.Description,-25} | {template.MainCategory,-19} | {template.Author}");
                }
            }
            else
            {
                Console.WriteLine("No templates found.");
            }
            
            return 0;
        }
        
        var argList = args.ToList();
        var newSiteName = string.Empty;
        var templateName = string.Empty;
        var directory = GetPathFromArgs(args);

        var result = 0;
        
        var logger = GetLogger(args);

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

            result = await templateService.CloneTemplateAsync(newSiteName, directory, repoUrl: url);
        }
        else
        {
            var templateFlagArg = argList.FindIndex(arg => arg.StartsWith("--template") || arg.StartsWith("-t"));

            if (templateFlagArg > 0)
            {
                // get the template name
                templateName = argList[templateFlagArg + 1];
                result = await templateService.CloneTemplateAsync(templateName, directory);
            }
            else
            {
                // don't use a template, just create a new Blazor site and initialise
                logger.LogInformation("No template specified, creating a new Blazor WASM site using the default template.");
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo("dotnet", "new blazorwasm")
                };
                
                var processResult = process.Start();
                
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var initResult = await SiteGenerator.InitAsync($"{newSiteName}.csproj", true, logger);

                    if (initResult == 0)
                    {
                        logger.LogInformation("✅ New site {newSiteName} created successfully.", newSiteName);
                    }
                    else
                    {
                        logger.LogError("Failed to create new Blake site");
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

        var newResult = await SiteGenerator.NewSiteAsync(newSiteName, templateName, directory, logger);

        if (newResult != 0)
        {
            logger.LogError("Failed to create new Blake site");
        }
        else
        {
            logger.LogInformation("✅ New site {newSiteName} created successfully.", newSiteName);
        }
        
        return newResult;
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

    private static ILogger GetLogger(string[] args)
    {
        var level = LogLevel.Warning;
        
        if (args.Contains("--verbosity") || args.Contains("-v"))
        {
            var argList = args.ToList();
            var verbosityIndex = argList.FindIndex(arg => arg is "--verbosity" or "-v");
            
            if (verbosityIndex >= 0)
            {
                if (verbosityIndex + 1 < argList.Count)
                {
                    var levelString = argList[verbosityIndex + 1];
                    
                    if (!Enum.TryParse<LogLevel>(levelString, out var parsedLevel))
                    {
                        Console.WriteLine($"⚠️ Invalid verbosity level: {levelString}. Using default level: Warning.");
                        parsedLevel = LogLevel.Warning;
                    }
                    
                    level = parsedLevel;
                }
                else
                {
                    Console.WriteLine("⚠️ Missing verbosity level after --verbosity or -v. Using default level: Warning.");
                    level = LogLevel.Warning;
                }
            }
        }
        
        // Add a default logger
        var logger = LoggerFactory.Create(c =>
        {
            c.AddConsole();
            c.SetMinimumLevel(level);
        });
        
        return logger.CreateLogger("Blake");
    }
}
