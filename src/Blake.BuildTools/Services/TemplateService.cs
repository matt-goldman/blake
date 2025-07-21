using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;

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
        return templates.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || t.ShortName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<int> CloneTemplateAsync (string name, string? destinationPath = null, Guid? templateId = null, string? repoUrl = null)
    {
        string templateName;

        var requiresGitRestore = false;
        
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

        // backup existing git folder and files first
        if (Directory.Exists(destinationPath ?? string.Empty) && Directory.GetFiles(destinationPath ?? string.Empty, ".git", SearchOption.AllDirectories).Length != 0)
        {
            Console.WriteLine("Existing .git directory found. Backing it up before cloning the new template...");
            var backupPath = Path.Combine(destinationPath ?? string.Empty, ".git_backup");
            
            BackupGitFiles(destinationPath ?? string.Empty, backupPath);

            requiresGitRestore = true;
        }

        Console.WriteLine($"🛠️  Creating new site from template '{templateName}'...");
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName                = "git",
                Arguments               = $"clone {repoUrl} {destinationPath ?? string.Empty}",
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
        var newSiteDirectory = destinationPath ?? Directory.GetCurrentDirectory();
        if (Directory.Exists(newSiteDirectory))
        {
            // Remove the .git directory to avoid confusion
            var gitDirectory = Path.Combine(newSiteDirectory, ".git");
            if (!Directory.Exists(gitDirectory)) return 0;
            Directory.Delete(gitDirectory, true);
            var gitFiles = new[] { ".gitignore", ".gitattributes" };
            foreach (var gitFile in gitFiles)
            {
                var gitFilePath = Path.Combine(newSiteDirectory, gitFile);
                if (File.Exists(gitFilePath))
                {
                    File.Delete(gitFilePath);
                    Console.WriteLine($"Removed {gitFile} from the cloned template.");
                }
            }
            Console.WriteLine("Removed .git directory from the cloned template.");
        }
        else
        {
            Console.WriteLine($"Template directory '{newSiteDirectory}' does not exist.");
            return -1;
        }

        if (requiresGitRestore)
        {
            Console.WriteLine("Restoring the original .git directory from backup...");
            var backupPath = Path.Combine(destinationPath ?? string.Empty, ".git_backup");
            RestoreGitFiles(destinationPath ?? string.Empty, backupPath);
        }

        return 0;
    }

    private void BackupGitFiles(string sourcePath, string backupPath)
    {
        if (!Directory.Exists(sourcePath)) return;
        
        // Create the backup directory if it doesn't exist
        Directory.CreateDirectory(backupPath);
        
        // Copy .git folder and other git related files to the backup folder
        var gitDirectory = Path.Combine(sourcePath, ".git");
        if (Directory.Exists(gitDirectory))
        {
            Directory.Move(gitDirectory, Path.Combine(backupPath, ".git"));
            Console.WriteLine("Existing .git directory backed up successfully.");
        }
        
        // also backup .gitignore and other git related files
        var gitFiles = new[] { ".gitignore", ".gitattributes" };
        foreach (var gitFile in gitFiles)
        {
            var sourceFilePath = Path.Combine(sourcePath, gitFile);
            if (File.Exists(sourceFilePath))
            {
                var backupFilePath = Path.Combine(backupPath, gitFile);
                File.Copy(sourceFilePath, backupFilePath, true);
                Console.WriteLine($"Backed up {gitFile} to {backupFilePath}");
            }
        }
    }

    private void RestoreGitFiles(string sourcePath, string backupPath)
    {
        if (!Directory.Exists(backupPath)) return;
        
        // Restore the .git directory from the backup folder
        var gitBackupDirectory = Path.Combine(backupPath, ".git");
        if (Directory.Exists(gitBackupDirectory))
        {
            Directory.Move(gitBackupDirectory, Path.Combine(sourcePath, ".git"));
            Console.WriteLine("Restored .git directory from backup successfully.");
        }
        
        // also restore .gitignore and other git related files
        var gitFiles = new[] { ".gitignore", ".gitattributes" };
        foreach (var gitFile in gitFiles)
        {
            var backupFilePath = Path.Combine(backupPath, gitFile);
            if (File.Exists(backupFilePath))
            {
                var sourceFilePath = Path.Combine(sourcePath, gitFile);
                File.Copy(backupFilePath, sourceFilePath, true);
                Console.WriteLine($"Restored {gitFile} from backup to {sourceFilePath}");
            }
        }
    }
}