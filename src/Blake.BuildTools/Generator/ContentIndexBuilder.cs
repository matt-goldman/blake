using System.Text;

namespace Blake.BuildTools.Generator;

public static class ContentIndexBuilder
{
    public static void WriteIndex(string outputPath, List<PageMetadata> allPages)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Blake.Types;");
        sb.AppendLine("namespace Blake.Generated;");
        sb.AppendLine("public static partial class GeneratedContentIndex");
        sb.AppendLine("{");
        sb.AppendLine("    public static partial List<PageMetadata> GetPages() => new()");
        sb.AppendLine("    {");

        foreach (var page in allPages)
        {
            sb.AppendLine("        new PageMetadata");
            sb.AppendLine("        {");
            sb.AppendLine($"            Title = @\"{page.Title}\",");
            sb.AppendLine($"            Slug = @\"{page.Slug}\",");
            sb.AppendLine($"            Description = @\"{page.Description}\",");
            if (page.Date.HasValue)
                sb.AppendLine($"            Date = new DateTime({page.Date.Value.Year}, {page.Date.Value.Month}, {page.Date.Value.Day}),");
            sb.AppendLine($"            Draft = {page.Draft.ToString().ToLowerInvariant()},");
            sb.AppendLine($"            IconIdentifier = @\"{page.IconIdentifier}\",");
            sb.AppendLine($"            Tags = new List<string> {{ {string.Join(", ", page.Tags.Select(t => $"\"{t}\""))} }}");
            sb.AppendLine("        },");
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
    public static partial List<PageMetadata> GetPages();
}";
        var filePath = Path.Combine(outputPath, "GeneratedContentIndex.Partial.cs");
        File.WriteAllText(filePath, generatedContentIndex);
    }
}
