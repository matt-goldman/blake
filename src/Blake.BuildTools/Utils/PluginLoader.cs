//using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

namespace Blake.BuildTools.Utils;

internal record NuGetPluginInfo(string PackageName, string Version, string DllPath);
internal record ProjectRefPluginInfo(string ProjectPath, string DllPath);

internal static class PluginLoader
{
    internal static List<PluginContext> LoadPlugins(string directory, string config, ILogger? logger)
    {
        var plugins = new List<PluginContext>();

        if (!Directory.Exists(directory))
        {
            return plugins;
        }

        var csprojFile = Directory.GetFiles(directory, "*.csproj").FirstOrDefault();

        if (csprojFile == null)
        {
            logger?.LogWarning("No .csproj file found in the specified directory.");
            return plugins;
        }

        var doc = new XDocument();
        try
        {
            doc = XDocument.Load(csprojFile);
        }
        catch (Exception ex)
        {
            logger?.LogError("Error loading .csproj file: {message}, {error}", ex.Message, ex);
            return plugins;
        }

        var fullCsprojPath = Path.GetFullPath(csprojFile);

        // Get plugin information first
        var nugetPlugins = GetNuGetPluginInfo(doc);
        var projectPlugins = GetProjectRefPluginInfo(doc, directory, config);

        // Check if all plugins are already valid (DLLs exist and have correct versions)
        bool needsRestore = !AreAllPluginsValid(nugetPlugins, projectPlugins, logger);

        if (needsRestore)
        {
            logger?.LogDebug("Some plugin DLLs are missing or outdated, running dotnet restore...");
            
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
                logger?.LogError("dotnet restore failed with exit code {exitCode}.", process.ExitCode);
                logger?.LogDebug(process.StandardOutput.ReadToEnd());
                logger?.LogError(process.StandardError.ReadToEnd());
                return plugins;
            }
        }
        else
        {
            logger?.LogDebug("All plugin DLLs are present and valid, skipping dotnet restore.");
        }

        LoadNuGetPlugins(nugetPlugins, plugins, logger);
        LoadProjectRefPlugins(projectPlugins, plugins, config, logger);

        return plugins;
    }

    private static List<NuGetPluginInfo> GetNuGetPluginInfo(XDocument project)
    {
        // Find the target framework dynamically from the .csproj file
        var targetFramework = project.Descendants("TargetFramework")
            .Select(tf => tf.Value)
            .FirstOrDefault() ?? "net9.0";

        var userHomeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var globalPackagesFolder = Path.Combine(userHomeDirectory, ".nuget", "packages");

        return project.Descendants("PackageReference")
            .Where(p => p.Attribute("Include")?.Value.StartsWith("BlakePlugin.", StringComparison.OrdinalIgnoreCase) == true)
            .Where(p => !string.IsNullOrWhiteSpace(p.Attribute("Version")?.Value))
            .Select(p =>
            {
                var packageName = p.Attribute("Include")!.Value;
                var packageVersion = p.Attribute("Version")!.Value;
                var packageNameLower = packageName.ToLowerInvariant();

                var dllPath = Path.Combine(
                    globalPackagesFolder,
                    packageNameLower,
                    packageVersion,
                    "lib",
                    targetFramework, // e.g. "net9.0"
                    $"{packageName}.dll" // case-sensitive match here
                );

                return new NuGetPluginInfo(packageName, packageVersion, dllPath);
            })
            .ToList();
    }

    private static List<ProjectRefPluginInfo> GetProjectRefPluginInfo(XDocument project, string projectDirectory, string configuration)
    {
        var projectReferences = project.Descendants("ProjectReference")
            .Select(p => p.Attribute("Include")?.Value)
            .Where(path => path is not null && Path.GetFileName(path).StartsWith("BlakePlugin."))
            .Select(path => Path.GetFullPath(Path.Combine(projectDirectory, path!)))
            .ToList();

        if (projectReferences.Count == 0)
        {
            return new List<ProjectRefPluginInfo>();
        }

        // Find the target framework dynamically from the .csproj file
        var targetFramework = project.Descendants("TargetFramework")
            .Select(tf => tf.Value)
            .FirstOrDefault() ?? "net9.0";

        return projectReferences.Select(pluginProject =>
        {
            var pluginName = Path.GetFileNameWithoutExtension(pluginProject);
            var outputPath = Path.Combine(Path.GetDirectoryName(pluginProject)!, "bin", configuration, targetFramework, $"{pluginName}.dll");
            return new ProjectRefPluginInfo(pluginProject, outputPath);
        }).ToList();
    }

    private static bool IsNuGetPluginValid(NuGetPluginInfo plugin, ILogger? logger)
    {
        if (!File.Exists(plugin.DllPath))
        {
            logger?.LogDebug("NuGet plugin DLL not found: {dllPath}", plugin.DllPath);
            return false;
        }

        try
        {
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(plugin.DllPath);
            var fileVersion = fileVersionInfo.FileVersion;
            
            if (string.IsNullOrEmpty(fileVersion))
            {
                logger?.LogDebug("No file version found for NuGet plugin: {dllPath}", plugin.DllPath);
                return true; // Assume valid if no version info
            }

            // For NuGet packages, the file version should match the package version
            if (VersionUtils.AreVersionsCompatible(plugin.Version, fileVersion))
            {
                return true;
            }

            logger?.LogDebug("Version mismatch for NuGet plugin {packageName}: expected {expectedVersion}, found {actualVersion}", 
                plugin.PackageName, plugin.Version, fileVersion);
            return false;
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "Error checking version for NuGet plugin: {dllPath}", plugin.DllPath);
            return false; // Assume invalid if we can't check version
        }
    }

    private static bool AreAllPluginsValid(List<NuGetPluginInfo> nugetPlugins, List<ProjectRefPluginInfo> projectPlugins, ILogger? logger)
    {
        // Check NuGet plugins (with version validation)
        foreach (var plugin in nugetPlugins)
        {
            if (!IsNuGetPluginValid(plugin, logger))
            {
                return false;
            }
        }

        // Check project reference plugins (just existence)
        foreach (var plugin in projectPlugins)
        {
            if (!File.Exists(plugin.DllPath))
            {
                logger?.LogDebug("Project reference plugin DLL not found: {dllPath}", plugin.DllPath);
                return false;
            }
        }

        return true;
    }

    private static void LoadNuGetPlugins(List<NuGetPluginInfo> nugetPlugins, List<PluginContext> plugins, ILogger? logger)
    {
        var validPluginFiles = nugetPlugins
            .Where(p => File.Exists(p.DllPath))
            .Select(p => p.DllPath)
            .ToList();

        if (validPluginFiles.Count > 0)
        {
            logger?.LogInformation("Found {pluginCount} NuGet plugins in the .csproj file.", validPluginFiles.Count);
            LoadPluginDLLs(validPluginFiles, plugins, logger);
        }
        else
        {
            logger?.LogInformation("No NuGet plugins found in the .csproj file.");
        }
    }

    private static void LoadProjectRefPlugins(List<ProjectRefPluginInfo> projectPlugins, List<PluginContext> plugins, string configuration, ILogger? logger)
    {
        if (projectPlugins.Count == 0)
        {
            logger?.LogDebug("No project references found in the .csproj file.");
            return;
        }

        var dllFilePaths = new List<string>();

        foreach (var plugin in projectPlugins)
        {
            // Check if DLL exists, if not, build the project
            if (!File.Exists(plugin.DllPath))
            {
                logger?.LogDebug("Project reference plugin DLL not found, building: {projectPath}", plugin.ProjectPath);
                
                var result = Process.Start(new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{plugin.ProjectPath}\" -c {configuration}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                });

                result?.WaitForExit();

                if (result?.ExitCode != 0)
                {
                    logger?.LogError("Failed to build {pluginProject}", plugin.ProjectPath);
                    continue;
                }
            }

            if (File.Exists(plugin.DllPath))
            {
                dllFilePaths.Add(plugin.DllPath);
            }
        }

        if (dllFilePaths.Count > 0)
        {
            logger?.LogInformation("Found {dllFilePathsCount} project references in the .csproj file.", dllFilePaths.Count);
            LoadPluginDLLs(dllFilePaths, plugins, logger);
        }
        else
        {
            logger?.LogDebug("No project references found in the .csproj file.");
        }
    }

    private static void LoadPluginDLLs(List<string> files, List<PluginContext> plugins, ILogger? logger)
    {
        foreach (var file in files)
        {
            if (!File.Exists(file))
            {
                logger?.LogError("Plugin file {file} does not exist.", file);
                continue;
            }

            var pluginName = Path.GetFileNameWithoutExtension(file);

            try
            {
                // Create a new load context for this plugin to resolve its dependencies
                var loadContext = new PluginLoadContext(file);
                var assembly = loadContext.LoadFromAssemblyPath(file);
                
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
                logger?.LogError("Error loading plugin from {file}: {message}", file, ex.Message);
                logger?.LogDebug(ex, "Full error details for plugin {file}", file);
            }
        }
    }
}

internal record PluginContext(string PluginName, IBlakePlugin Plugin);
