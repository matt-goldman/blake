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

    public static MarkdownPipelineBuilder SetupContainerRenderers(this MarkdownPipelineBuilder builder, bool? useDefaultRenderers = true)
    {
        // Add the custom container parser if not already present
        builder.Extensions.AddIfNotAlready<CustomContainerExtension>();

        if (useDefaultRenderers == true)
        {
            // Add the default container renderer if not already present
            builder.Extensions.AddIfNotAlready<DefaultContainerExtension>();
        }
        else
        {
            // get the DefaultContainerExtension and remove it
            var defaultContainerExtension = builder.Extensions.OfType<DefaultContainerExtension>().FirstOrDefault();

            if (defaultContainerExtension != null)
            {
                builder.Extensions.Remove(defaultContainerExtension);
            }

            // TODO: Add the RazorContainerExtension (TBW)
        }

        return builder;
    }
}
