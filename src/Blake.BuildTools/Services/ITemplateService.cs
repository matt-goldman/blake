namespace Blake.BuildTools.Services;

public interface ITemplateService
{
    Task<IEnumerable<SiteTemplate>> GetTemplatesAsync();
    
    Task<SiteTemplate?> GetTemplateAsync(Guid id);
    Task<SiteTemplate?> GetTemplateAsync(string name);

    Task<int> CloneTemplateAsync(string name, Guid? templateId = null, string? repoUrl = null);
}