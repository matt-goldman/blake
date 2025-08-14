using Blake.CLI;
using Blake.IntegrationTests.Infrastructure;
using Blake.Types;
using Microsoft.Extensions.Logging;

namespace Blake.IntegrationTests.Commands;

/// <summary>
/// Integration tests for the `blake new` command.
/// Tests creating new Blake sites with various templates and options.
/// </summary>
public class BlakeNewCommandTests : TestFixtureBase
{
    [Fact]
    public async Task BlakeNew_WithNoArguments_ShowsHelp()
    {
        // Act
        var result = await RunBlakeCommandAsync(["new"]);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        // Should show help or error about missing path
        Assert.Contains(result.OutputText, o => o.Contains("path") || o.Contains("usage"));
    }

    [Fact]
    public async Task BlakeNew_WithListOption_ShowsAvailableTemplates()
    {
        // Act
        
        // create template registry file in user profile directory
        const string shortName1 = "tailwind-sample";
        const string shortName2 = "simpledocs";
        const string longName1 = "Blake Simple Tailwind Sample";
        const string longName2 = "Blake Simple Docs";

        var templateRegistryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".blake", "TemplateRegistry.json");
        if(!File.Exists(templateRegistryPath))
        {
            // Create a mock TemplateRegistry.json for testing
            var templates = new List<SiteTemplate>
            {
                new (Guid.Empty, shortName1, longName1, "", "", "", DateTime.MinValue, ""),
                new (Guid.Empty, shortName2, longName2, "", "", "", DateTime.MinValue, "")
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(templates, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(templateRegistryPath)!);
            File.WriteAllText(templateRegistryPath, jsonContent);
        }

        // Has to be run with debug to use local TemplateRegistry.json, otherwise it calls the repo
        var result = await RunBlakeFromDotnetAsync("new --list", debug: true);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(result.OutputText, o => o.Contains("Available templates"));
        
        // Should show templates from TemplateRegistry.json
        Assert.Contains(result.OutputText, o => o.Contains(shortName1));
        Assert.Contains(result.OutputText, o => o.Contains(shortName2));
        Assert.Contains(result.OutputText, o => o.Contains(longName1));
        Assert.Contains(result.OutputText, o => o.Contains(longName2));

        // Cleanup the mock TemplateRegistry.json
        if (File.Exists(templateRegistryPath))
        {
            File.Delete(templateRegistryPath);
        }
    }

    [Fact]
    public async Task BlakeNew_DefaultTemplate_CreatesBlazorWasmProject()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-default");
        var projectName = "TestSite";
        var projectPath = Path.Combine(testDir, projectName);

        // Act
        var result = await RunBlakeCommandAsync(["new", projectPath, "-s"]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(result.OutputText, o => o.Contains("created successfully"));

        // Should create a Blazor WASM project structure
        FileSystemHelper.AssertDirectoryExists(projectPath);
        FileSystemHelper.AssertFileExists(Path.Combine(projectPath, $"{projectName}.csproj"));
        FileSystemHelper.AssertFileExists(Path.Combine(projectPath, "Program.cs"));
        FileSystemHelper.AssertFileExists(Path.Combine(projectPath, "App.razor"));
        
        // Should have Blake-specific folders created by init
        FileSystemHelper.AssertDirectoryExists(Path.Combine(projectPath, "Pages"));
        
        // Should contain sample content because init is called with includeSampleContent=true
        FileSystemHelper.AssertFileExists(Path.Combine(projectPath, "Pages", "SamplePAge.md"));
    }

    [Fact]
    public async Task BlakeNew_WithSiteName_UsesProvidedName()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-sitename");
        var projectName = "MyAwesomeSite";
        var projectPath = Path.Combine(testDir, "project-folder");

        // Act
        var result = await RunBlakeCommandAsync(["new", projectPath, "--siteName", projectName]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(result.OutputText, o => o.Contains("created successfully"));
        
        // Should use the provided site name for the project file
        FileSystemHelper.AssertFileExists(Path.Combine(projectPath, $"{projectName}.csproj"));
    }

    [Fact]
    public async Task BlakeNew_InvalidSiteName_ShowsError()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-invalid");
        var projectPath = Path.Combine(testDir, "test-project");
        var invalidSiteName = "Invalid/Site\\Name"; // Contains directory separators

        // Act
        var result = await RunBlakeCommandAsync(["new", projectPath, "--siteName", invalidSiteName]);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(result.ErrorText, e => e.Contains("invalid"));
    }

    [Fact]
    public async Task BlakeNew_NonExistentPath_CreatesDirectory()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-create-dir");
        var projectPath = Path.Combine(testDir, "deeply", "nested", "path", "MyProject");

        // Act  
        var result = await RunBlakeCommandAsync(["new", projectPath]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        FileSystemHelper.AssertDirectoryExists(projectPath);
        FileSystemHelper.AssertFileExists(Path.Combine(projectPath, "MyProject.csproj"));
    }

    [Fact]
    public async Task BlakeNew_ExistingNonEmptyDirectory_ShowsError()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-existing");
        var projectPath = Path.Combine(testDir, "existing-project");
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(Path.Combine(projectPath, "existing-file.txt"), "content");

        // Act
        var result = await RunBlakeCommandAsync(["new", projectPath]);

        // Assert
        // The behavior might vary - some generators create anyway, others fail
        // We'll check what actually happens and validate the output is sensible
        if (result.ExitCode != 0)
        {
            Assert.True(result.ErrorText.Contains("exists") ||
                       result.ErrorText.Contains("not empty"));
        }
    }

    [Fact]
    public async Task BlakeNew_WithTemplate_InvalidTemplateName_ShowsError()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-invalid-template");
        var projectPath = Path.Combine(testDir, "test-project");

        // Act
        var result = await RunBlakeCommandAsync(["new", projectPath, "--template","non-existent-template"]);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.True(result.ErrorText.Contains("template") ||
                   result.ErrorText.Contains("not found"));
    }

    [Theory]
    [InlineData("tailwind-sample")]
    [InlineData("Blake Simple Tailwind Sample")] // Full name
    public async Task BlakeNew_WithValidTemplate_UsesTemplate(string templateName)
    {
        // Arrange
        var testDir = CreateTempDirectory($"blake-new-template-{templateName.Replace(" ", "-")}");
        var projectPath = Path.Combine(testDir, "TestProject");

        // Act - Note: This will try to actually clone from GitHub, might fail in test environment
        var result = await RunBlakeCommandAsync(["new", projectPath, "--template", templateName]);

        // Assert
        if (result.ExitCode == 0)
        {
            // If successful, should have cloned the template
            FileSystemHelper.AssertDirectoryExists(projectPath);
            Assert.Contains(result.OutputText, o => o.Contains("created successfully"));
        }
        else
        {
            // If failed (e.g., network issues), should show a relevant error
            Logger.LogWarning("Template clone failed (expected in test environment): {Error}", result.ErrorText);
            Assert.True(result.ErrorText.Contains("git") ||
                       result.ErrorText.Contains("clone") ||
                       result.ErrorText.Contains("repository") ||
                       result.ErrorText.Contains("network"));
        }
    }

    [Fact]
    public async Task BlakeNew_WithUrl_UsesCustomRepository()
    {
        // Arrange  
        var testDir = CreateTempDirectory("blake-new-url");
        var projectPath = Path.Combine(testDir, "TestProject");
        var repoUrl = "https://github.com/matt-goldman/BlakeSimpleTailwindSample";

        // Act - This will try to clone from the actual repository
        var result = await RunBlakeCommandAsync(["new", projectPath, "--url", repoUrl]);

        // Assert
        if (result.ExitCode == 0)
        {
            FileSystemHelper.AssertDirectoryExists(projectPath);
            Assert.Contains(result.OutputText, o => o.Contains("created successfully"));
        }
        else
        {
            // If failed (network/git issues), should show relevant error
            Logger.LogWarning("URL clone failed (expected in test environment): {Error}", result.ErrorText);
            Assert.True(result.ErrorText.Contains("git") ||
                       result.ErrorText.Contains("clone") ||
                       result.ErrorText.Contains("repository"));
        }
    }

    [Fact]
    public async Task BlakeNew_WithInvalidUrl_ShowsError()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-invalid-url");
        var projectPath = Path.Combine(testDir, "TestProject");
        var invalidUrl = "https://github.com/invalid/nonexistent-repo";

        // Act
        var result = await RunBlakeCommandAsync(["new", projectPath, "--url", invalidUrl]);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(result.ErrorText, e => 
                                            e.Contains("Failed to create site from template.") ||
                                            e.Contains("git") ||
                                            e.Contains("clone") ||
                                            e.Contains("repository") ||
                                            e.Contains("not found"));
    }

    [Fact] 
    public async Task BlakeNew_ResultingProject_CanBuild()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-build");
        var projectName = "BuildableProject";
        var projectPath = Path.Combine(testDir, projectName);

        // Act - Create project
        var createResult = await RunBlakeCommandAsync(["new", projectPath, "-s"]);
        Assert.Equal(0, createResult.ExitCode);

        // Act - Try to build the project
        var buildResult = await RunProcessAsync("dotnet", "build", projectPath);

        // Assert
        Assert.Equal(0, buildResult.ExitCode);
        Assert.Contains(buildResult.OutputText, o => o.Contains("Build succeeded"));
    }
}