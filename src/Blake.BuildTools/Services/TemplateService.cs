using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;

namespace Blake.BuildTools.Services;

public class TemplateService : ITemplateService
{
    private static HttpClient? _httpClient;
    
    private static HttpClient CurrentClient => _httpClient ??= new HttpClient { BaseAddress = new Uri("https://raw.githubusercontent.com") };
    
    public async Task<IEnumerable<SiteTemplate>> GetTemplatesAsync(CancellationToken cancellationToken)
    {
#if DEBUG
        var userProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var templateFilePath = Path.Combine(userProfileFolder, ".blake", "TemplateRegistry.json");
        var fileContent = await File.ReadAllTextAsync(templateFilePath, cancellationToken);
        var templates = System.Text.Json.JsonSerializer.Deserialize<TemplateRegistry>(fileContent);
        return templates?.Templates ?? [];
#else
        var templates = await CurrentClient.GetFromJsonAsync<TemplateRegistry>("matt-goldman/blake/refs/heads/main/TemplateRegistry.json", cancellationToken);
        return templates?.Templates ?? [];
#endif
    }

    public async Task<SiteTemplate?> GetTemplateAsync(Guid id, CancellationToken cancellationToken)
    {
        var templates = await GetTemplatesAsync(cancellationToken);
        return templates.FirstOrDefault(t => t.Id == id);
    }

    public async Task<SiteTemplate?> GetTemplateAsync(string name, CancellationToken cancellationToken)
    {
        var templates = await GetTemplatesAsync(cancellationToken);
        return templates.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || t.ShortName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<int> CloneTemplateAsync (string name, ILogger logger, CancellationToken cancellationToken, string? destinationPath = null, Guid? templateId = null, string? repoUrl = null)
    {
        string templateName;

        var requiresGitRestore = false;
        
        if (repoUrl is null)
        {
            var template = templateId.HasValue
                ? await GetTemplateAsync(templateId.Value, cancellationToken)
                : await GetTemplateAsync(name, cancellationToken);
        
            if (template == null)
            {
                logger.LogError("Template with ID '{templateId}' or name '{name}' not found.", templateId, name);
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
            logger.LogDebug("Existing .git directory found. Backing it up before cloning the new template...");
            var backupPath = Path.Combine(destinationPath ?? string.Empty, ".git_backup");

            BackupGitFiles(destinationPath ?? string.Empty, backupPath, logger);

            requiresGitRestore = true;
        }

        logger.LogInformation("🛠️  Creating new site from template '{templateName}'...", templateName);
        
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
        logger.LogInformation("Cloning template from {repoUrl}...", repoUrl);
        
        process.Start();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        { 
            logger.LogWarning("Git clone operation was cancelled for template '{templateName}'.", templateName);
            return -1;
        }
        
        if (process.ExitCode != 0)
        {
            var errorOutput = await process.StandardError.ReadToEndAsync();
            logger.LogError("Failed to clone template '{templateName}': {errorOutput}", templateName, errorOutput);
            return -1;
        }
        
        logger.LogInformation("Template '{templateName}' cloned successfully to '{destinationPath}'.", templateName, destinationPath ?? Directory.GetCurrentDirectory());

        logger.LogDebug("Cleaning up the cloned directory...");
        
        var newSiteDirectory = destinationPath ?? Directory.GetCurrentDirectory();
        
        if (Directory.Exists(newSiteDirectory))
        {
            // Remove the .git directory to avoid confusion
            var gitDirectory = Path.Combine(newSiteDirectory, ".git");
            
            if (!Directory.Exists(gitDirectory)) return 0;

            RemoveReadOnlyGitDirectory(gitDirectory, logger);
            
            var gitFiles = new[] { ".gitignore", ".gitattributes" };
            
            foreach (var gitFile in gitFiles)
            {
                var gitFilePath = Path.Combine(newSiteDirectory, gitFile);

                if (File.Exists(gitFilePath))
                {
                    File.Delete(gitFilePath);
                    logger.LogDebug("Removed {gitFile} from the cloned template.", gitFile);
                }
            }
            logger.LogDebug("Removed .git directory from the cloned template.");
        }
        else
        {
            logger.LogError("Template directory '{newSiteDirectory}' does not exist.", newSiteDirectory);
            return -1;
        }

        if (requiresGitRestore)
        {
            logger.LogDebug("Restoring the original .git directory from backup...");
            var backupPath = Path.Combine(destinationPath ?? string.Empty, ".git_backup");
            RestoreGitFiles(destinationPath ?? string.Empty, backupPath, logger);
        }

        return 0;
    }

    private static void BackupGitFiles(string sourcePath, string backupPath, ILogger? logger)
    {
        if (!Directory.Exists(sourcePath)) return;
        
        // Create the backup directory if it doesn't exist
        Directory.CreateDirectory(backupPath);
        
        // Copy .git folder and other git related files to the backup folder
        var gitDirectory = Path.Combine(sourcePath, ".git");
        if (Directory.Exists(gitDirectory))
        {
            Directory.Move(gitDirectory, Path.Combine(backupPath, ".git"));
            logger.LogDebug("Existing .git directory backed up successfully.");
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
                logger.LogDebug("Backed up {gitFile} to {backupFilePath}", gitFile, backupFilePath);
            }
        }
    }

    private static void RestoreGitFiles(string sourcePath, string backupPath, ILogger? logger)
    {
        if (!Directory.Exists(backupPath)) return;
        
        // Restore the .git directory from the backup folder
        var gitBackupDirectory = Path.Combine(backupPath, ".git");
        if (Directory.Exists(gitBackupDirectory))
        {
            Directory.Move(gitBackupDirectory, Path.Combine(sourcePath, ".git"));
            logger.LogDebug("Restored .git directory from backup successfully.");
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
                logger.LogDebug("Restored {gitFile} from backup to {sourceFilePath}", gitFile, sourceFilePath);
            }
        }
    }

    private static void RemoveReadOnlyGitDirectory(string gitDirectory, ILogger logger)
    {
        logger.LogDebug("Removing read-only attributes from .git directory before deletion...");
        
        try
        {
            // Recursively remove read-only attributes from all files and directories
            SetDirectoryWritable(gitDirectory);

            Directory.Delete(gitDirectory, true);
            
            logger.LogDebug("Successfully removed .git directory after clearing read-only attributes.");
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to delete .git directory: {Error}", ex.Message);
            // Don't fail the entire operation if we can't clean up the .git directory
        }
    }

    private static void SetDirectoryWritable(string directoryPath)
    {
        SetDirectoryWritableRecursive(new DirectoryInfo(directoryPath));
    }

    private static void SetDirectoryWritableRecursive(DirectoryInfo directoryInfo)
    {
        // Remove read-only attribute from the directory itself
        if (directoryInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
        {
            directoryInfo.Attributes &= ~FileAttributes.ReadOnly;
        }

        // Process all files in the current directory
        foreach (var file in directoryInfo.GetFiles())
        {
            if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                file.Attributes &= ~FileAttributes.ReadOnly;
            }
        }

        // Recursively process all subdirectories
        foreach (var subDirectory in directoryInfo.GetDirectories())
        {
            SetDirectoryWritableRecursive(subDirectory);
        }
    }
}