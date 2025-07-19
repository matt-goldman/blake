namespace Blake.BuildTools.Generator;

public static class RazorPageBuilder
{
    public static string BuildRazorPage(string templatePath, string markdownContent, string route, PageModel metadata)
    {
        var templateSource = File.ReadAllText(templatePath);

        // Example: Replace placeholders (can be improved with more sophisticated templating)
        templateSource = templateSource.Replace("@Body", markdownContent);
        templateSource = templateSource.Replace("@Route", route);
        templateSource = templateSource.Replace("@Title", metadata.Title);
        templateSource = templateSource.Replace("@Description", metadata.Description);
        templateSource = templateSource.Replace("@Published", metadata.Date?.ToString("D"));

        var pageDirective = $"@page \"{route}\"";

        return $"{pageDirective}\n\n{templateSource}";
    }
}
