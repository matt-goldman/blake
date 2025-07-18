using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Blake.MarkdownParser;

public class RazorContainerRenderer : HtmlObjectRenderer<CustomContainer>
{
    protected override void Write(HtmlRenderer renderer, CustomContainer container)
    {
        if (string.IsNullOrEmpty(container.Info)) return;

        var containerName = GetContainerName(container.Info);

        renderer.Write($"<{containerName}>").WriteLine();

        renderer.WriteChildren(container);

        renderer.Write($"</{containerName}>").WriteLine();

    }

    private string GetContainerName(string info)
    {
        // Convert to PascalCase
        var containerName = char.ToUpper(info[0]) + info.Substring(1).ToLowerInvariant();
        return $"{containerName}Container";
    }
}
