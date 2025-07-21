using System.Diagnostics;
using System.Net.Http.Json;

namespace Blake.BuildTools.Services;

public class TemplateService : ITemplateService
{
    private static HttpClient? _httpClient;
    
    private static HttpClient CurrentClient => _httpClient ??= new HttpClient { BaseAddress = new Uri("https://raw.githubusercontent.com/matt-goldman/blake/refs/heads/main") };
    
    public async Task<IEnumerable<SiteTemplate>> GetTemplatesAsync()
    {
#if DEBUG
        var userProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var templateFilePath = Path.Combine(userProfileFolder, ".blake", "TemplateRegistry.json");
        var fileContent = await File.ReadAllTextAsync(templateFilePath);
        var templates = System.Text.Json.JsonSerializer.Deserialize<TemplateRegistry>(fileContent);
        return templates?.Templates ?? [];
#else
        var templates = await CurrentClient.GetFromJsonAsync<TemplateRegistry>("TemplateRegistry.json");
        return templates?.Templates ?? [];
#endif
    }

    public async Task<SiteTemplate?> GetTemplateAsync(Guid id)
    {
        var templates = await GetTemplatesAsync();
        return templates.FirstOrDefault(t => t.Id == id);
    }

    public async Task<SiteTemplate?> GetTemplateAsync(string name)
    {
        var templates = await GetTemplatesAsync();
        return templates.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<int> CloneTemplateAsync (string name, Guid? templateId = null, string? repoUrl = null)
    {
        string templateName;
        
        if (repoUrl is null)
        {
            var template = templateId.HasValue
                ? await GetTemplateAsync(templateId.Value)
                : await GetTemplateAsync(name);
        
            if (template == null)
            {
                Console.WriteLine($"Template with ID '{templateId}' or name '{name}' not found.");
                return -1;
            }

            repoUrl = template.RepositoryUrl;
            templateName = template.Name;
        }
        else
        {
            templateName = repoUrl.Split('/').Last().Replace(".git", "");
        }
        
        Console.WriteLine($"🛠️  Creating new site from template '{templateName}'...");
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName                = "git",
                Arguments               = $"clone {repoUrl}",
                RedirectStandardOutput  = true,
                RedirectStandardError   = true,
                UseShellExecute         = false,
                CreateNoWindow          = true
            }
        };
        
        // Start the git clone process
        Console.WriteLine($"Cloning template from {repoUrl}...");
        
        var cloneResult = process.Start();

        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            Console.WriteLine("Failed to clone template. Error output:");
            var errorOutput = await process.StandardError.ReadToEndAsync();
            Console.WriteLine(errorOutput);
            return -1;
        }
        
        Console.WriteLine($"Template '{templateName}' copied successfully.");
        
        Console.WriteLine("Cleaning up the cloned directory...");
        var currentDirectory = Directory.GetCurrentDirectory();
        var templateDirectory = Path.Combine(currentDirectory, templateName);
        if (Directory.Exists(templateDirectory))
        {
            // Remove the .git directory to avoid confusion
            var gitDirectory = Path.Combine(templateDirectory, ".git");
            if (!Directory.Exists(gitDirectory)) return 0;
            Directory.Delete(gitDirectory, true);
            Console.WriteLine("Removed .git directory from the cloned template.");
        }
        else
        {
            Console.WriteLine($"Template directory '{templateDirectory}' does not exist.");
            return -1;
        }

        return 0;
    }
}