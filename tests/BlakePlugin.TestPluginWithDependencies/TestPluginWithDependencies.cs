using Blake.BuildTools;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlakePlugin.TestPluginWithDependencies;

public class TestPluginWithDependencies : IBlakePlugin
{
    public Task BeforeBakeAsync(BlakeContext context, ILogger? logger = null)
    {
        logger?.LogInformation("TestPluginWithDependencies: BeforeBakeAsync called");
        
        // Use SixLabors.ImageSharp to test dependency loading
        using var image = new Image<Rgba32>(100, 100);
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    row[x] = Color.Blue;
                }
            }
        });
        
        var testMessage = $"Plugin with dependencies loaded successfully. Created {image.Width}x{image.Height} image. PageCount: {context.MarkdownPages.Count}";
        
        logger?.LogInformation("TestPluginWithDependencies: {TestMessage}", testMessage);
        
        // Create a marker file to prove the plugin ran with dependencies
        var testFilePath = Path.Combine(context.ProjectPath, ".plugin-with-deps-before-bake.txt");
        File.WriteAllText(testFilePath, testMessage);
        
        return Task.CompletedTask;
    }

    public Task AfterBakeAsync(BlakeContext context, ILogger? logger = null)
    {
        logger?.LogInformation("TestPluginWithDependencies: AfterBakeAsync called with {PageCount} generated pages", context.GeneratedPages.Count);
        
        // Use SixLabors.ImageSharp again to ensure dependency is still available
        using var image = new Image<Rgba32>(50, 50);
        var testMessage = $"Plugin dependencies working in AfterBakeAsync. Created {image.Width}x{image.Height} image. GeneratedPageCount: {context.GeneratedPages.Count}";
        
        var testFilePath = Path.Combine(context.ProjectPath, ".plugin-with-deps-after-bake.txt");
        File.WriteAllText(testFilePath, testMessage);
        
        return Task.CompletedTask;
    }
}