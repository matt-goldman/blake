using Blake.BuildTools;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlakePlugin.TestPluginWithDependencies;

public class TestPluginWithDependencies : IBlakePlugin
{
    public Task BeforeBakeAsync(BlakeContext context, ILogger? logger = null)
    {
        logger?.LogInformation("TestPluginWithDependencies: BeforeBakeAsync called");
        
        // Use Newtonsoft.Json to test dependency loading
        var testObject = new { Message = "Plugin with dependencies loaded successfully", PageCount = context.MarkdownPages.Count };
        var serialized = JsonConvert.SerializeObject(testObject);
        
        logger?.LogInformation("TestPluginWithDependencies: Serialized data: {SerializedData}", serialized);
        
        // Create a marker file to prove the plugin ran with dependencies
        var testFilePath = Path.Combine(context.ProjectPath, ".plugin-with-deps-before-bake.txt");
        File.WriteAllText(testFilePath, serialized);
        
        return Task.CompletedTask;
    }

    public Task AfterBakeAsync(BlakeContext context, ILogger? logger = null)
    {
        logger?.LogInformation("TestPluginWithDependencies: AfterBakeAsync called with {PageCount} generated pages", context.GeneratedPages.Count);
        
        // Use Newtonsoft.Json again to ensure dependency is still available
        var testObject = new { Message = "Plugin dependencies working in AfterBakeAsync", GeneratedPageCount = context.GeneratedPages.Count };
        var serialized = JsonConvert.SerializeObject(testObject);
        
        var testFilePath = Path.Combine(context.ProjectPath, ".plugin-with-deps-after-bake.txt");
        File.WriteAllText(testFilePath, serialized);
        
        return Task.CompletedTask;
    }
}