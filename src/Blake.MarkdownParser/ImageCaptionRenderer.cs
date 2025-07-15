using System.Text;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;

namespace Blake.MarkdownParser;


public class ImageCaptionRenderer : HtmlObjectRenderer<LinkInline>
{
    protected override void Write(HtmlRenderer renderer, LinkInline link)
    {
        if (link.IsImage)
        {

            var caption = link.FirstChild?.ToString() ?? "";
            var src = link.Url?.Trim() ?? "";

            // Read GenericAttributes
            var attrs = link.TryGetAttributes();

            // Build attribute string
            var attrString = BuildAttributeString(attrs);

            // Fallback style if no explicit width or style
            var hasWidth = attrs?.Properties?.Any(p => p.Key == "width") ?? false;
            var hasStyle = attrs?.Properties?.Any(p => p.Key == "style") ?? false;
            if (!hasWidth && !hasStyle)
            {
                attrString += " style=\"max-width:100%;height:auto;\"";
            }

            renderer
                .Write("<figure>")
                .Write($"<img src=\"{src}\" alt=\"{caption}\" {attrString} />")
                .Write($"<figcaption style=\"margin-left:auto;margin-right:auto;font-style:italic;text-align:center;\">{caption}</figcaption>")
                .Write("</figure>");
        }
        else
        {
            // Fallback: render link <a href="...">content</a>
            var href = link.Url?.Trim() ?? "#";
            renderer.Write($"<a href=\"{href}\">");
            renderer.WriteChildren(link);
            renderer.Write("</a>");
        }
    }

    private string BuildAttributeString(HtmlAttributes? attrs)
    {
        if (attrs == null) return "";

        var sb = new StringBuilder();

        foreach (var prop in attrs?.Properties?? [])
        {
            var safeName = HtmlEscape(prop.Key);
            var safeValue = HtmlEscape(prop.Value ?? "");
            sb.Append($" {safeName}=\"{safeValue}\"");
        }

        return sb.ToString();
    }

    private static string HtmlEscape(string input) =>
        System.Net.WebUtility.HtmlEncode(input);
}
