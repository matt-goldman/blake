using Microsoft.Extensions.Logging;

namespace Blake.BuildTools.Services;

public interface ITemplateService
{
    Task<IEnumerable<SiteTemplate>> GetTemplatesAsync(CancellationToken cancellationToken);
    
    Task<SiteTemplate?> GetTemplateAsync(Guid id, CancellationToken cancellationToken);
    Task<SiteTemplate?> GetTemplateAsync(string name, CancellationToken cancellationToken);

    Task<int> CloneTemplateAsync(string name, ILogger logger, CancellationToken cancellationToken, string? destinationPath = null, Guid? templateId = null, string? repoUrl = null);
}