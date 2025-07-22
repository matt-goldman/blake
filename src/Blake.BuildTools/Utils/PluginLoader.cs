using System.Reflection;

namespace Blake.BuildTools.Utils;

internal static class PluginLoader
{
    internal static List<PluginContext> LoadPlugins(string directory)
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

        var doc = new System.Xml.Linq.XDocument();
        try
        {
            doc = System.Xml.Linq.XDocument.Load(csprojFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading .csproj file: {ex.Message}");
            return plugins;
        }

        var pluginFiles = doc.Descendants("PackageReference")
            .Where(x => x.Attribute("Include")?.Value.StartsWith("BlakePlugin.") == true)
            .Select(x => x.Attribute("Version")?.Value)
            .Where(version => !string.IsNullOrEmpty(version))
            .Select(version => Path.Combine(directory, "bin", "Debug", "net9.0", $"BlakePlugin.{version}.dll"))
            .Where(File.Exists)
            .ToList();

        foreach (var file in pluginFiles)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine($"Plugin file {file} does not exist.");
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

        return plugins;
    }
}

internal record PluginContext(string PluginName, IBlakePlugin Plugin);
