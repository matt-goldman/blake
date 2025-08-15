using System.Text;
using Microsoft.Extensions.Logging;

namespace Blake.BuildTools.Generator;

public static class ContentIndexBuilder
{
    public static void WriteIndex(string outputPath, List<PageModel> allPages, bool continueOnError, ILogger logger)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Blake.Types;");
        sb.AppendLine("namespace Blake.Generated;");
        sb.AppendLine("public static partial class GeneratedContentIndex");
        sb.AppendLine("{");
        sb.AppendLine("    public static partial List<PageModel> GetPages() => new()");
        sb.AppendLine("    {");

        foreach (var page in allPages)
        {
            var tags = page.Tags ?? [];
            
            try
            {
                sb.AppendLine("        new PageModel");
                sb.AppendLine("        {");
                sb.AppendLine($"            Id = @\"{page.Id}\",");
                sb.AppendLine($"            Title = @\"{page.Title}\",");
                sb.AppendLine($"            Slug = @\"{page.Slug}\",");
                sb.AppendLine($"            Description = @\"{page.Description}\",");
                if (page.Date.HasValue)
                    sb.AppendLine(
                        $"            Date = new DateTime({page.Date.Value.Year}, {page.Date.Value.Month}, {page.Date.Value.Day}),");
                sb.AppendLine($"            Draft = {page.Draft.ToString().ToLowerInvariant()},");
                sb.AppendLine($"            IconIdentifier = @\"{page.IconIdentifier}\",");
                if (tags.Count > 0)
                {
                    sb.AppendLine(
                        $"            Tags = new List<string> {{ {string.Join(", ", tags.Select(t => $"\"{t}\""))} }},");
                }
                else
                {
                    sb.AppendLine($"            Tags = new List<string> {{ }},");
                }
                sb.AppendLine($"            Image = @\"{page.Image}\",");
                sb.AppendLine("            Metadata = new Dictionary<string, string>");
                sb.AppendLine("            {");
                foreach (var kvp in page.Metadata)
                {
                    sb.AppendLine($"                [\"{kvp.Key}\"] = \"{kvp.Value}\",");
                }

                sb.AppendLine("            }");
                sb.AppendLine("        },");
            }
            catch (Exception e)
            {
                if (continueOnError)
                {
                    logger.LogWarning(e, "Failed to write page to index. Continuing with next page. Details: ID: {pageId}, Title: {pageTitle}", page.Id, page.Title);
                }
                else
                {
                    logger.LogError(e, "Failed to write page to index. Details: ID: {pageId}, Title: {pageTitle}", page.Id, page.Title);
                    throw;
                }
            }
        }

        sb.AppendLine("    };\n}");

        var filePath = Path.Combine(outputPath, "GeneratedContentIndex.cs");
        File.WriteAllText(filePath, sb.ToString());
    }

    public static void WriteIndexPartial(string outputPath)
    {
        const string generatedContentIndex = @"using Blake.Types;

namespace Blake.Generated;

public static partial class GeneratedContentIndex
{
    public static partial List<PageModel> GetPages();
}";
        var filePath = Path.Combine(outputPath, "GeneratedContentIndex.Partial.cs");
        File.WriteAllText(filePath, generatedContentIndex);
    }
}
