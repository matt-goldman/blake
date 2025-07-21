using Blake.BuildTools.Generator;
using System.Diagnostics;
using Blake.BuildTools.Services;
using Blake.Types;

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
        Console.WriteLine();
        Console.WriteLine("  new <PATH>           Generates a new Blake site");
        Console.WriteLine("                       Options:");
        Console.WriteLine("                         --template, -t   The name of the template to create a site from (optional). Uses the default Blazor WASM template if not specified.");
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
        
        // Check if the path is a .csproj file
        if (Path.GetExtension(targetPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(targetPath))
            {
                Console.WriteLine($"Error: Project file '{targetPath}' does not exist.");
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
                Console.WriteLine("Error: No .csproj file found in the specified path.");
                return 1;
            }
            projectFile = csprojFiles[0];
        }
        
        if (!Directory.Exists(targetPath))
        {
            Console.WriteLine($"Error: Path '{targetPath}' does not exist.");
            return 1;
        }
        
        Console.WriteLine($"🛠  Initializing Blake in: {targetPath}");
        
        // Check if the project is a Blazor WASM app
        if (!File.Exists(projectFile))
        {
            Console.WriteLine($"Error: Project file '{projectFile}' does not exist.");
            return 1;
        }

        var includeSampleContent = args.Contains("--includeSampleContent") || args.Contains("-s");

        return await SiteGenerator.InitAsync(projectFile, includeSampleContent);
    }

    private static async Task<int> BakeBlakeAsync(string[] args)
    {
        // get target path or use current directory
        var targetPath = GetPathFromArgs(args);

        if (!Directory.Exists(targetPath))
        {
            Console.WriteLine($"Error: Path '{targetPath}' does not exist.");
            return 1;
        }

        Console.WriteLine($"🛠  Starting build for: {targetPath}");

        // TODO: Add blake.config.json support
        var options = new GenerationOptions
        {
            ProjectPath = targetPath,
            OutputPath = Path.Combine(targetPath, ".generated"),
            UseDefaultRenderers = !args.Contains("--disableDefaultRenderers") && !args.Contains("-dr")
        };

        await SiteGenerator.BuildAsync(options);

        Console.WriteLine("✅ Build completed successfully.");
        
        return 0;
    }

    public static async Task<int> ServeBakeAsync(string[] args)
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

        Process.Start(psi)?.WaitForExit();
        return 0;
    }

    private static async Task<int> NewSiteAsync(string[] args)
    {
        var templateService = new TemplateService();
        
        if (args[1] == "--list")
        {
            var templates = await templateService.GetTemplatesAsync();
            
            var templateList = templates.OrderBy(t => t.Name).ToList() ?? new List<SiteTemplate>();
            
            // TODO: Improve this with Specter.Console or similar
            if (templateList.Count > 0)
            {
                Console.WriteLine("Available templates:");
                Console.WriteLine("Template Name        | Description         | Main Category       | Author");
                Console.WriteLine("---------------------|---------------------|---------------------|---------------------");
                foreach (var template in templateList)
                {
                    Console.WriteLine($"{template.Name,-20} | {template.Description,-20} | {template.MainCategory,-20} | {template.Author}");
                    Console.WriteLine($"Last Updated: {template.LastUpdated:yyyy-MM-dd HH:mm:ss} | Repository: {template.RepositoryUrl}");
                    Console.WriteLine(new string('-', 80));
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

        if (argList.Contains("--siteName") || argList.Contains("-sn"))
        {
            var siteNameFlagIndex = argList.FindIndex(arg => arg is "--siteName" or "-sn");

            if (siteNameFlagIndex >= 0)
            {
                newSiteName = argList[siteNameFlagIndex + 1];
            }
        }
        else
        {
            var directoryParts = directory.Split(Path.DirectorySeparatorChar);
            newSiteName = directoryParts[^1];
        }

        if (argList.Contains("--url") || argList.Contains("-u"))
        {
            var urlFlagArgIndex = argList.FindIndex(arg => arg.StartsWith("--url") || arg.StartsWith("-u"));
            var url = argList[urlFlagArgIndex + 1];

            if (string.IsNullOrWhiteSpace(url))
            {
                Console.WriteLine($"❌ Error: Url '{url}' is missing or invalid.");
                return 1;
            }

            result = await templateService.CloneTemplateAsync(newSiteName, repoUrl: url);
        }
        else
        {
            var templateFlagArg = argList.FindIndex(arg => arg.StartsWith("--template") || arg.StartsWith("-t"));

            if (templateFlagArg > 0)
            {
                // get the template name
                templateName = argList[templateFlagArg + 1];
                result = await templateService.CloneTemplateAsync(templateName);
            }
            else
            {
                // don't use a template, just create a new Blazor site and initialise
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo("dotnet", "new blazorwasm")
                };
                
                var processResult = process.Start();
                
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var initResult = await SiteGenerator.InitAsync($"{newSiteName}.csproj", true);

                    var resultMessage = initResult == 0 ? $"✅ New site {newSiteName} created successfully." : "❌ Failed to create new Blake site";
                    
                    Console.WriteLine(resultMessage);
                    return initResult;;
                }
            }
        }

        if (result < 1)
        {
            Console.WriteLine("❌ Failed to create site from template.");
            return -1;
        }

        var newResult = await SiteGenerator.NewSiteAsync(newSiteName, templateName);
        
        var finalMessage = newResult == 0 ? $"✅ New site {newSiteName} created successfully." : "❌ Failed to create new Blake site";
        
        Console.WriteLine(finalMessage);
        return newResult;
    }

    private static string GetPathFromArgs(string[] args)
    {
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]) && !args[1].StartsWith('-'))
        {
            return args[1];
        }

        return Directory.GetCurrentDirectory();
    }
}
