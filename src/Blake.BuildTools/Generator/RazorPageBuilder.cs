namespace Blake.BuildTools.Generator;

public static class RazorPageBuilder
{
    public static string BuildRazorPage(string templatePath, string markdownContent, string route, PageModel page)
    {
        var templateSource = File.ReadAllText(templatePath);

        // Example: Replace placeholders (can be improved with more sophisticated templating)
        templateSource = templateSource.Replace("@Body", markdownContent);
        templateSource = templateSource.Replace("@Route", route);
        templateSource = templateSource.Replace("@Title", page.Title);
        templateSource = templateSource.Replace("@Description", page.Description);
        templateSource = templateSource.Replace("@Published", page.Date?.ToString("D"));
        templateSource = templateSource.Replace("@Id", page.Id);

        var pageDirective = $"@page \"{route}\"";

        return $"{pageDirective}\n\n{templateSource}";
    }
}
