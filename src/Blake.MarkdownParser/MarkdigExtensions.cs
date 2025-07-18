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

    public static MarkdownPipelineBuilder SetupContainerRenderers(this MarkdownPipelineBuilder builder, bool? useDefaultRenderers = true, bool? useRazorContainers = false)
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
        }

        if (useRazorContainers == true)
        {
            // Add the Razor container extension if not already present
            builder.Extensions.AddIfNotAlready<RazorContainerExtension>();
        }
        else
        {
            // get the RazorContainerExtension and remove it
            var razorContainerExtension = builder.Extensions.OfType<RazorContainerExtension>().FirstOrDefault();
            if (razorContainerExtension != null)
            {
                builder.Extensions.Remove(razorContainerExtension);
            }
        }

        return builder;
    }
}
