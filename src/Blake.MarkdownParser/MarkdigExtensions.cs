using Markdig;
using Markdig.Extensions.CustomContainers;

namespace Blake.MarkdownParser;

public static class MarkdigExtensions
{
    public static MarkdownPipelineBuilder SetupContainerRenderers(this MarkdownPipelineBuilder builder, bool? useDefaultRenderers = true, bool? useRazorContainers = false)
    {
        // Add the custom container parser if not already present
        builder.Extensions.AddIfNotAlready<CustomContainerExtension>();

        builder.Extensions.AddIfNotAlready(new DefaultContainerExtension(useDefaultRenderers??true, useRazorContainers??false));

        return builder;
    }
}
