using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Prism;

namespace Blake.MarkdownParser;

public static class MarkdigExtensions
{
    public static MarkdownPipelineBuilder UsePrism(this MarkdownPipelineBuilder pipeline)
    {
        PrismOptions options = new PrismOptions();
        pipeline.Extensions.Add(new PrismExtension(options));
        return pipeline;
    }

    public static MarkdownPipelineBuilder UsePrism(this MarkdownPipelineBuilder pipeline, PrismOptions options)
    {
        if (options == null)
        {
            options = new PrismOptions();
        }
        pipeline.Extensions.Add(new PrismExtension(options));
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseAncRenderers(this MarkdownPipelineBuilder builder)
    {
        // Add the custom container parser if not already present
        builder.Extensions.AddIfNotAlready<CustomContainerExtension>();
        builder.Extensions.AddIfNotAlready<BlakeContainerExtension>();
        return builder;
    }
}
