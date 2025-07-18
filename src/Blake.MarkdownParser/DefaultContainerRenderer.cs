using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Blake.MarkdownParser;

public class DefaultContainerRenderer : HtmlObjectRenderer<CustomContainer>
{
    private static readonly string[] _containerTypes = ["exercise", "warning", "tip", "info", "note"];

    protected override void Write(HtmlRenderer renderer, CustomContainer container)
    {
        string containerType = container.Info ?? string.Empty;

        if (_containerTypes.Contains(containerType))
        {
            var info = new ContainerInfo(string.Empty, string.Empty, string.Empty);

            switch (containerType)
            {
                case "exercise":
                    info = new ContainerInfo("alert-success", "bi-check-circle-fill", "Exercise:");
                    break;
                case "warning":
                    info = new ContainerInfo("alert-warning", "bi-exclamation-triangle-fill", "Warning:");
                    break;
                case "tip":
                    info = new ContainerInfo("alert-secondary", "bi-lightbulb-fill", "Tip:");
                    break;
                case "note":
                    info = new ContainerInfo("alert-primary", "bi-pencil-fill", "Note:");
                    break;
                case "info":
                    info = new ContainerInfo("alert-info", "bi-info-circle-fill", "Info:");
                    break;
                default:
                    break;
            }

            if (renderer.EnableHtmlForBlock)
            {
                renderer.Write($"<div class=\"alert {info.AlertClass}\" role=\"alert\">").WriteLine();
                renderer.Write("<div class=\"d-flex align-items-center\">").WriteLine();
                renderer.Write($"<i class=\"{info.IconClass} flex-shrink-0 me-2\"aria-label=\"{info.Title}\"></i>").WriteLine();
                renderer.Write($"<h5>{info.Title}</h5>").WriteLine();
                renderer.Write("</div>").WriteLine();
            }

            renderer.WriteChildren(container); // Render the inner content

            if (renderer.EnableHtmlForBlock)
            {
                renderer.Write("</div>").WriteLine();
            }
        }
        else if (container.Info == "answer")
        {
            renderer.Write("<details>").WriteLine();
            renderer.Write("<summary>Reveal answer:</summary>").WriteLine();
            renderer.Write("<div class=\"px-4 pb-2\">").WriteLine();
            renderer.WriteChildren(container); // Render the inner content
            renderer.Write("</div>").WriteLine();
            renderer.Write("</details>").WriteLine();
        }
        else
        {
            renderer.Write(container);
        }
    }
}

internal record ContainerInfo(string AlertClass, string IconClass, string Title);
