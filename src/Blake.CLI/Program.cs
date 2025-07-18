using Blake.BuildTools.Generator;
using System.Diagnostics;

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
            default:
                await Console.Error.WriteLineAsync($"Unknown option: {option}");
        
                return 1;
        }
    }
    
    private static void ShowHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  blake [options] <path>");
        Console.WriteLine("Options:");
        Console.WriteLine("  init <PATH>                    Configure an existing Blazor WASM app for Blake.");
        Console.WriteLine("  bake <PATH>                    Generate static content for a Blake site.");
        Console.WriteLine("  --IncludeSampleContent, -s     Includes a sample page and updates the nav menu (for the default Blazor template) when initializing a Blake site");
        Console.WriteLine("  --help                         Show this help message.");
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

        var includeSampleContent = args.Contains("--IncludeSampleContent") || args.Contains("-s");

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
            OutputPath = Path.Combine(targetPath, ".generated")
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


    private static string GetPathFromArgs(string[] args)
    {
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]) && !args[1].StartsWith('-'))
        {
            return args[1];
        }

        return Directory.GetCurrentDirectory();
    }
}
