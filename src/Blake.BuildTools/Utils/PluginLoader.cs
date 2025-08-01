//using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

namespace Blake.BuildTools.Utils;

internal static class PluginLoader
{
    internal static List<PluginContext> LoadPlugins(string directory, string config)
    {
        var plugins = new List<PluginContext>();

        if (!Directory.Exists(directory))
        {
            return plugins;
        }

        var csprojFile = Directory.GetFiles(directory, "*.csproj").FirstOrDefault();

        if (csprojFile == null)
        {
            Console.WriteLine("No .csproj file found in the specified directory.");
            return plugins;
        }

        var doc = new XDocument();
        try
        {
            doc = XDocument.Load(csprojFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading .csproj file: {ex.Message}");
            return plugins;
        }

        var fullCsprojPath = Path.GetFullPath(csprojFile);

        // ensure dotnet restore has been run
        Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"restore {fullCsprojPath}",
                WorkingDirectory = directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Console.WriteLine($"dotnet restore failed with exit code {process.ExitCode}.");
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            Console.WriteLine(process.StandardError.ReadToEnd());
            return plugins;
        }

        LoadNuGetPlugins(doc, plugins);
        LoadProjectRefPlugins(doc, directory, config, plugins);

        return plugins;
    }

    private static void LoadNuGetPlugins(XDocument project, List<PluginContext> plugins)
    {
        // Find the target framework dynamically from the .csproj file
        var targetFramework = project.Descendants("TargetFramework")
            .Select(tf => tf.Value)
            .FirstOrDefault() ?? "net9.0";

        var userHomeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var globalPackagesFolder = Path.Combine(userHomeDirectory, ".nuget", "packages");

        var pluginFiles = project.Descendants("PackageReference")
            .Where(p => p.Attribute("Include")?.Value.StartsWith("BlakePlugin.", StringComparison.OrdinalIgnoreCase) == true)
            .Where(p => !string.IsNullOrWhiteSpace(p.Attribute("Version")?.Value))
            .Select(p =>
            {
                var packageName = p.Attribute("Include")!.Value;
                var packageVersion = p.Attribute("Version")!.Value;
                var packageNameLower = packageName.ToLowerInvariant();

                return Path.Combine(
                    globalPackagesFolder,
                    packageNameLower,
                    packageVersion,
                    "lib",
                    targetFramework, // e.g. "net9.0"
                    $"{packageName}.dll" // case-sensitive match here
                );
            })
            .Where(File.Exists)
            .ToList();

        if (pluginFiles.Count > 0)
        {
            Console.WriteLine($"Found {pluginFiles.Count} NuGet plugins in the .csproj file.");
            LoadPluginDLLs(pluginFiles, plugins);
        }
        else
        {
            Console.WriteLine("No NuGet plugins found in the .csproj file.");
        }
    }

    private static void LoadProjectRefPlugins(XDocument project, string projectDirectory, string configuration, List<PluginContext> plugins)
    {
        var projectReferences = project.Descendants("ProjectReference")
            .Select(p => p.Attribute("Include")?.Value)
            .Where(path => path is not null && Path.GetFileName(path).StartsWith("BlakePlugin."))
            .Select(path => Path.GetFullPath(Path.Combine(projectDirectory, path!)))
            .ToList();

        var dllFilePaths = new List<string>();

        if (projectReferences.Count > 0)
        {
            // Find the target framework dynamically from the .csproj file
            var targetFramework = project.Descendants("TargetFramework")
                .Select(tf => tf.Value)
                .FirstOrDefault() ?? "net9.0";

            foreach (var pluginProject in projectReferences)
            {
                var result = Process.Start(new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{pluginProject}\" -c {configuration}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                });

                result?.WaitForExit();

                if (result?.ExitCode != 0)
                {
                    Console.WriteLine($"⚠️ Failed to build {pluginProject}");
                    continue;
                }

                var pluginName = Path.GetFileNameWithoutExtension(pluginProject);
                var outputPath = Path.Combine(Path.GetDirectoryName(pluginProject)!, "bin", configuration, targetFramework, $"{pluginName}.dll");

                dllFilePaths.Add(outputPath);
            }

            if (dllFilePaths.Count > 0)
            {
                Console.WriteLine($"Found {dllFilePaths.Count} project references in the .csproj file.");
                LoadPluginDLLs(dllFilePaths, plugins);
            }
            else
            {
                Console.WriteLine("No project references found in the .csproj file.");
            }
        }
        else
        {
            Console.WriteLine("No project references found in the .csproj file.");
        }
    }

    private static void LoadPluginDLLs(List<string> files, List<PluginContext> plugins)
    {
        foreach (var file in files)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine($"❌ Plugin file {file} does not exist.");
                continue;
            }

            var pluginName = Path.GetFileNameWithoutExtension(file);

            try
            {
                var assembly = Assembly.LoadFrom(file);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IBlakePlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in pluginTypes)
                {
                    if (Activator.CreateInstance(type) is IBlakePlugin plugin)
                    {
                        plugins.Add(new(pluginName, plugin));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading plugin from {file}: {ex.Message}");
            }
        }
    }
}

internal record PluginContext(string PluginName, IBlakePlugin Plugin);
