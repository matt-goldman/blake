using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Parsers;
using Markdig.Renderers;

namespace Blake.MarkdownParser;

public class DefaultContainerExtension(bool UseDefaultRenderers, bool UseRazorRenderers) : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        // Remove any existing CustomContainerParser
        var existing = pipeline.BlockParsers
            .OfType<CustomContainerParser>()
            .FirstOrDefault();

        if (existing != null)
        {
            pipeline.BlockParsers.Remove(existing);
        }

        // Add safe version
        if (!pipeline.BlockParsers.OfType<SafeContainerParser>().Any())
        {
            var fenced = pipeline.BlockParsers
                .FirstOrDefault(p => p is FencedCodeBlockParser);

            if (fenced != null)
            {
                var index = pipeline.BlockParsers.IndexOf(fenced);
                // Add our parser right AFTER the code block parser
                pipeline.BlockParsers.Insert(index + 1, new SafeContainerParser());
            }
            else
            {
                // fallback: add to end
                pipeline.BlockParsers.Add(new SafeContainerParser());
            }

        }
    }


    public void Setup(MarkdownPipeline pipeline, IMarkdownParser renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        if (renderer is HtmlRenderer htmlRenderer)
        {
            if (!htmlRenderer.ObjectRenderers.Contains<DefaultContainerRenderer>())
            {
                // Must be inserted before CodeBlockRenderer
                htmlRenderer.ObjectRenderers.Insert(0, new DefaultContainerRenderer(UseDefaultRenderers, UseRazorRenderers));
            }
        }
    }
}
