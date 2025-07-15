using Markdig;
using Markdig.Renderers;

namespace Blake.MarkdownParser;

public class ImageCaptionExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        // no-op
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer)
        {
            htmlRenderer.ObjectRenderers.Insert(0, new ImageCaptionRenderer());
        }
    }
}

public static class ImageCaptionExtensions
{
    public static MarkdownPipelineBuilder UseImageCaptions(this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.Add(new ImageCaptionExtension());
        return pipeline;
    }
}
