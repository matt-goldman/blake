using System.Reflection;
using System.Runtime.Loader;

namespace Blake.BuildTools.Utils;

/// <summary>
/// A custom AssemblyLoadContext that provides isolated plugin loading with dependency resolution.
/// Each plugin gets its own load context to avoid dependency conflicts.
/// </summary>
internal class PluginLoadContext(string pluginPath) : AssemblyLoadContext(isCollectible: true)
{
    private readonly AssemblyDependencyResolver _resolver = new(pluginPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Force Blake assemblies to be loaded in the default context to avoid interface compatibility issues
        if (assemblyName.Name != null && assemblyName.Name.StartsWith("Blake."))
        {
            return null; // Use default load context for Blake assemblies
        }
        
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // Return null to fall back to default load context for shared dependencies
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}