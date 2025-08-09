using Blake.BuildTools;
using Microsoft.Extensions.Logging;
using Markdig;

namespace BlakePlugin.TestPlugin;

public class TestPlugin : IBlakePlugin
{
    public Task BeforeBakeAsync(BlakeContext context, ILogger? logger = null)
    {
        logger?.LogInformation("TestPlugin: BeforeBakeAsync called with {PageCount} markdown pages", context.MarkdownPages.Count);
        
        // Add some test metadata to demonstrate plugin functionality
        foreach (var page in context.MarkdownPages)
        {
            // Create a test file to prove the plugin ran
            var testFilePath = Path.Combine(context.ProjectPath, ".plugin-before-bake.txt");
            File.WriteAllText(testFilePath, $"BeforeBakeAsync called at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }
        
        return Task.CompletedTask;
    }

    public Task AfterBakeAsync(BlakeContext context, ILogger? logger = null)
    {
        logger?.LogInformation("TestPlugin: AfterBakeAsync called with {PageCount} generated pages", context.GeneratedPages.Count);
        
        // Create a test file to prove the plugin ran
        var testFilePath = Path.Combine(context.ProjectPath, ".plugin-after-bake.txt");
        File.WriteAllText(testFilePath, $"AfterBakeAsync called at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        
        return Task.CompletedTask;
    }
}

public class FailingTestPlugin : IBlakePlugin
{
    public Task BeforeBakeAsync(BlakeContext context, ILogger? logger = null)
    {
        throw new InvalidOperationException("Test plugin intentionally failing");
    }

    public Task AfterBakeAsync(BlakeContext context, ILogger? logger = null)
    {
        return Task.CompletedTask;
    }
}

public class ContentModifyingPlugin : IBlakePlugin
{
    public Task BeforeBakeAsync(BlakeContext context, ILogger? logger = null)
    {
        logger?.LogInformation("ContentModifyingPlugin: Adding test metadata to pages");
        
        // Modify the pipeline to add custom processing - use a valid extension
        context.PipelineBuilder.UseEmphasisExtras();
        
        return Task.CompletedTask;
    }

    public Task AfterBakeAsync(BlakeContext context, ILogger? logger = null)
    {
        logger?.LogInformation("ContentModifyingPlugin: Processing generated pages");
        
        // Add custom metadata to generated pages
        foreach (var page in context.GeneratedPages)
        {
            page.Page.Metadata["ProcessedByPlugin"] = "ContentModifyingPlugin";
        }
        
        return Task.CompletedTask;
    }
}