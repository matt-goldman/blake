using Microsoft.Extensions.Logging;

namespace Blake.BuildTools.Services;

public interface ITemplateService
{
    Task<IEnumerable<SiteTemplate>> GetTemplatesAsync();
    
    Task<SiteTemplate?> GetTemplateAsync(Guid id);
    Task<SiteTemplate?> GetTemplateAsync(string name);

    Task<int> CloneTemplateAsync(string name, string? destinationPath = null, Guid? templateId = null, string? repoUrl = null, ILogger? logger = null);
}