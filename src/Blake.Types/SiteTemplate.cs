namespace Blake.Types;

public record SiteTemplate(
    Guid Id,
    string Name,
    string Description,
    string MainCategory,
    string Author,
    DateTime LastUpdated,
    string RepositoryUrl);

public record TemplateRegistry(IEnumerable<SiteTemplate> Templates);    