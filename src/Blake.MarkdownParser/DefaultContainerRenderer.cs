using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using System.Diagnostics;

namespace Blake.MarkdownParser;

public class DefaultContainerRenderer(bool UseDefaultRenderers, bool UseRazorRenderers) : HtmlObjectRenderer<CustomContainer>
{
    private static readonly string[] _containerTypes = ["exercise", "warning", "tip", "info", "note"];

    private readonly HashSet<CustomContainer> _rendered = new();

    protected override void Write(HtmlRenderer renderer, CustomContainer container)
    {
        if (container == null || _rendered.Contains(container))
        {
            Debug.WriteLine("⚠️ Skipping already-rendered container to avoid recursion.");
            return;
        }

        _rendered.Add(container);

        var containerHandled = false;

        if (UseDefaultRenderers)
        {
            containerHandled = RenderDefaultContainer(container, renderer);
        }

        if (containerHandled) return; // If we handled it with default rendering, exit early

        if (UseRazorRenderers)
        {
            RenderRazorContainer(container, renderer);
        }
        else if (container.Info == "child")
        {
            RenderChildContent(container, renderer);
        }
        else
        {
            renderer.Write(container);
        }
    }

    private string GetContainerName(string info)
    {
        // Convert to PascalCase
        var containerName = char.ToUpper(info[0]) + info.Substring(1).ToLowerInvariant();
        return $"{containerName}Container";
    }

    private void RenderChildContent(CustomContainer container, HtmlRenderer renderer)
    {
        foreach (var child in container)
        {
            if (ReferenceEquals(child, container))
            {
                Debug.WriteLine("❌ Detected self-referencing container block");
                continue; // or return;
            }

            Debug.WriteLine($"🧱 Rendering child of type: {child.GetType().Name}");


            renderer.Write(child);
        }
    }

    private bool RenderDefaultContainer(CustomContainer container, HtmlRenderer renderer)
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

            // Render the inner content
            RenderChildContent(container, renderer);


            if (renderer.EnableHtmlForBlock)
            {
                renderer.Write("</div>").WriteLine();
            }

            return true; // Indicate that we rendered a default container
        }
        else if (container.Info == "answer")
        {
            renderer.Write("<details>").WriteLine();
            renderer.Write("<summary>Reveal answer:</summary>").WriteLine();
            renderer.Write("<div class=\"px-4 pb-2\">").WriteLine();
            // Render the inner content
            RenderChildContent(container, renderer);

            renderer.Write("</div>").WriteLine();
            renderer.Write("</details>").WriteLine();

            return true; // Indicate that we rendered an answer container
        }

        return false; // Indicate that we did not render a default container
    }

    private void RenderRazorContainer(CustomContainer container, HtmlRenderer renderer)
    {
        if (string.IsNullOrEmpty(container.Info)) return;

        var containerName = GetContainerName(container.Info);

        renderer.Write($"<{containerName} {container.Arguments}>").WriteLine();

        renderer.WriteChildren(container);

        renderer.Write($"</{containerName}>").WriteLine();
    }
}

internal record ContainerInfo(string AlertClass, string IconClass, string Title);
