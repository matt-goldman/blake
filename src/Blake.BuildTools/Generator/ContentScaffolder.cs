using Blake.BuildTools.Utils;
using System.Globalization;

namespace Blake.BuildTools.Generator;

internal sealed record ContentScaffoldRequest(string ProjectPath, string ContentFolderPath, string Title, DateTime CreatedAtUtc);

internal sealed record ContentScaffoldResult(string ContentType, string OutputFilePath);

internal static class ContentScaffolder
{
    public static async Task<ContentScaffoldResult> CreateAsync(ContentScaffoldRequest request, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.ContentFolderPath);

        var contentType = InferContentType(request.ContentFolderPath, request.ProjectPath);
        var dateStamp = request.CreatedAtUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var id = Guid.NewGuid().ToString();
        var slug = Slugify(request.Title);
        var fileName = contentType == "post" ? $"{dateStamp}-{slug}.md" : $"{slug}.md";
        var outputFilePath = Path.Combine(request.ContentFolderPath, fileName);
        var counter = 2;
        while (File.Exists(outputFilePath))
        {
            var candidate = contentType == "post"
                ? $"{dateStamp}-{slug}-{counter}.md"
                : $"{slug}-{counter}.md";
            outputFilePath = Path.Combine(request.ContentFolderPath, candidate);
            counter++;
        }

        var templateFilePath = ResolveTemplatePath(contentType, request.ContentFolderPath, request.ProjectPath);
        var contentTemplate = templateFilePath is not null
            ? await File.ReadAllTextAsync(templateFilePath, cancellationToken)
            : GetDefaultContentTemplate(request.Title, dateStamp);

        var content = ApplyTemplateValues(contentTemplate, request.Title, dateStamp, slug, id);

        await File.WriteAllTextAsync(outputFilePath, content, cancellationToken);
        return new ContentScaffoldResult(contentType, outputFilePath);
    }

    private static string Slugify(string title)
    {
        var slugBuilder = new System.Text.StringBuilder();
        var lastWasDash = false;

        foreach (var c in title.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                slugBuilder.Append(c);
                lastWasDash = false;
                continue;
            }

            if (lastWasDash || slugBuilder.Length == 0) continue;
            slugBuilder.Append('-');
            lastWasDash = true;
        }

        var slug = slugBuilder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "untitled" : slug;
    }

    private static string GetDefaultContentTemplate(string title, string dateStamp)
    {
        var escapedTitle = EscapeForDoubleQuotedYaml(title);
        return $"""
                ---
                title: "{escapedTitle}"
                date: {dateStamp}
                description: ""
                ---

                # {title}

                """;
    }

    private static string EscapeForDoubleQuotedYaml(string input)
    {
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string InferContentType(string outputDirectory, string projectPath)
    {
        if (PathSegmentsContain(outputDirectory, "pages"))
        {
            return "page";
        }

        if (PathSegmentsContain(outputDirectory, "posts"))
        {
            return "post";
        }

        var hasPostTemplate = ResolveTemplatePath("post", outputDirectory, projectPath) is not null;
        var hasPageTemplate = ResolveTemplatePath("page", outputDirectory, projectPath) is not null;

        if (hasPageTemplate && !hasPostTemplate)
        {
            return "page";
        }

        return "post";
    }

    private static bool PathSegmentsContain(string path, string segment)
    {
        return path
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
            .Any(part => string.Equals(part, segment, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveTemplatePath(string contentType, string outputDirectory, string projectPath)
    {
        var candidates = new[]
        {
            Path.Combine(outputDirectory, $"{contentType}-template.md"),
            Path.Combine(outputDirectory, "template.md"),
            Path.Combine(projectPath, $"{contentType}-template.md"),
            Path.Combine(projectPath, "template.md")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string ApplyTemplateValues(string contentTemplate, string title, string dateStamp, string slug, string id)
    {
        var content = contentTemplate
            .Replace("{{title}}", title)
            .Replace("{{date}}", dateStamp)
            .Replace("{{slug}}", slug)
            .Replace("{{id}}", id);

        return FrontmatterHelper.UpdateFrontmatterValuesIfPresent(content, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["title"] = title,
            ["date"] = dateStamp,
            ["id"] = id
        });
    }
}
