using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using System.Text;
using System.Text.RegularExpressions;

namespace Blake.MarkdownParser;

public partial class ImageCaptionRenderer : HtmlObjectRenderer<LinkInline>
{
    private static readonly Regex _shorthandDimensionsRegex = ShorthandRegex();

    protected override void Write(HtmlRenderer renderer, LinkInline link)
    {
        if (link.IsImage)
        {
            var caption = link.FirstChild?.ToString() ?? "";
            var originalSrc = link.Url?.Trim() ?? "";

            // Parse shorthand dimensions from URL (e.g., "/image.png =200x300")
            var (src, shorthandWidth, shorthandHeight) = ParseShorthandDimensions(originalSrc);

            // Read GenericAttributes
            var attrs = link.TryGetAttributes();

            // Apply shorthand dimensions if no explicit width/height attributes are present
            if (attrs == null)
            {
                attrs = new HtmlAttributes();
            }

            // Only apply shorthand if no explicit dimensions are set via generic attributes
            var hasExplicitWidth = attrs.Properties?.Any(p => p.Key == "width") ?? false;
            var hasExplicitHeight = attrs.Properties?.Any(p => p.Key == "height") ?? false;

            if (!hasExplicitWidth && shorthandWidth.HasValue)
            {
                attrs.AddProperty("width", shorthandWidth.Value.ToString());
            }

            if (!hasExplicitHeight && shorthandHeight.HasValue)
            {
                attrs.AddProperty("height", shorthandHeight.Value.ToString());
            }

            // Build attribute string
            var attrString = BuildAttributeString(attrs);

            // Fallback style if no explicit width or style
            var hasWidth = attrs.Properties?.Any(p => p.Key == "width") ?? false;
            var hasStyle = attrs.Properties?.Any(p => p.Key == "style") ?? false;
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

        foreach (var prop in attrs?.Properties ?? [])
        {
            var safeName = HtmlEscape(prop.Key);
            var safeValue = HtmlEscape(prop.Value ?? "");
            sb.Append($" {safeName}=\"{safeValue}\"");
        }

        return sb.ToString();
    }

    private static (string cleanUrl, int? width, int? height) ParseShorthandDimensions(string url)
    {
        var match = _shorthandDimensionsRegex.Match(url);
        if (!match.Success)
        {
            return (url, null, null);
        }

        var cleanUrl = url.Substring(0, match.Index).Trim();

        var widthGroup = match.Groups[1];
        var heightGroup = match.Groups[2];

        int? width = null;
        int? height = null;

        if (widthGroup.Success && !string.IsNullOrEmpty(widthGroup.Value))
        {
            if (int.TryParse(widthGroup.Value, out var w))
            {
                width = w;
            }
        }

        if (heightGroup.Success && !string.IsNullOrEmpty(heightGroup.Value))
        {
            if (int.TryParse(heightGroup.Value, out var h))
            {
                height = h;
            }
        }

        return (cleanUrl, width, height);
    }

    private static string HtmlEscape(string input) =>
        System.Net.WebUtility.HtmlEncode(input);

    [GeneratedRegex(@"\s*=(\d*)?x(\d*)?\s*$", RegexOptions.Compiled)]
    private static partial Regex ShorthandRegex();
}