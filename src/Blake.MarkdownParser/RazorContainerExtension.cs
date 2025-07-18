using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;

namespace Blake.MarkdownParser;

public class RazorContainerExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.BlockParsers.Contains<CustomContainerParser>())
        {
            // Insert the parser before any other parsers
            pipeline.BlockParsers.Insert(0, new CustomContainerParser());
        }
    }
    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        if (renderer is HtmlRenderer htmlRenderer)
        {
            if (!htmlRenderer.ObjectRenderers.Contains<RazorContainerRenderer>())
            {
                // Must be inserted before CodeBlockRenderer
                htmlRenderer.ObjectRenderers.Insert(0, new RazorContainerRenderer());
            }
        }
    }
}