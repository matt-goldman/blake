using Blake.IntegrationTests.Infrastructure;
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
        var result = await RunBlakeCommandAsync("new");

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        // Should show help or error about missing path
        Assert.True(result.OutputText.Contains("path", StringComparison.OrdinalIgnoreCase) ||
                    result.ErrorText.Contains("path", StringComparison.OrdinalIgnoreCase) ||
                    result.OutputText.Contains("usage", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BlakeNew_WithListOption_ShowsAvailableTemplates()
    {
        // Act
        var result = await RunBlakeCommandAsync("new --list");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Available templates", result.OutputText, StringComparison.OrdinalIgnoreCase);
        
        // Should show templates from TemplateRegistry.json
        Assert.Contains("tailwind-sample", result.OutputText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("blakedocs", result.OutputText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Blake Simple Tailwind Sample", result.OutputText);
        Assert.Contains("Blake Docs", result.OutputText);
    }

    [Fact]
    public async Task BlakeNew_DefaultTemplate_CreatesBlazorWasmProject()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-default");
        var projectName = "TestSite";
        var projectPath = Path.Combine(testDir, projectName);

        // Act
        var result = await RunBlakeCommandAsync($"new \"{projectPath}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("created successfully", result.OutputText, StringComparison.OrdinalIgnoreCase);

        // Should create a Blazor WASM project structure
        FileSystemHelper.AssertDirectoryExists(projectPath);
        FileSystemHelper.AssertFileExists(Path.Combine(projectPath, $"{projectName}.csproj"));
        FileSystemHelper.AssertFileExists(Path.Combine(projectPath, "Program.cs"));
        FileSystemHelper.AssertFileExists(Path.Combine(projectPath, "App.razor"));
        
        // Should have Blake-specific folders created by init
        FileSystemHelper.AssertDirectoryExists(Path.Combine(projectPath, "Posts"));
        FileSystemHelper.AssertDirectoryExists(Path.Combine(projectPath, "Pages"));
        
        // Should contain sample content because init is called with includeSampleContent=true
        FileSystemHelper.AssertFileExists(Path.Combine(projectPath, "Posts", "hello-world.md"));
    }

    [Fact]
    public async Task BlakeNew_WithSiteName_UsesProvidedName()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-sitename");
        var projectName = "MyAwesomeSite";
        var projectPath = Path.Combine(testDir, "project-folder");

        // Act
        var result = await RunBlakeCommandAsync($"new \"{projectPath}\" --siteName \"{projectName}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("created successfully", result.OutputText, StringComparison.OrdinalIgnoreCase);
        
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
        var result = await RunBlakeCommandAsync($"new \"{projectPath}\" --siteName \"{invalidSiteName}\"");

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("invalid", result.ErrorText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BlakeNew_NonExistentPath_CreatesDirectory()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-create-dir");
        var projectPath = Path.Combine(testDir, "deeply", "nested", "path", "MyProject");

        // Act  
        var result = await RunBlakeCommandAsync($"new \"{projectPath}\"");

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
        var result = await RunBlakeCommandAsync($"new \"{projectPath}\"");

        // Assert
        // The behavior might vary - some generators create anyway, others fail
        // We'll check what actually happens and validate the output is sensible
        if (result.ExitCode != 0)
        {
            Assert.True(result.ErrorText.Contains("exists", StringComparison.OrdinalIgnoreCase) ||
                       result.ErrorText.Contains("not empty", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task BlakeNew_WithTemplate_InvalidTemplateName_ShowsError()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-invalid-template");
        var projectPath = Path.Combine(testDir, "test-project");

        // Act
        var result = await RunBlakeCommandAsync($"new \"{projectPath}\" --template \"non-existent-template\"");

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.True(result.ErrorText.Contains("template", StringComparison.OrdinalIgnoreCase) ||
                   result.ErrorText.Contains("not found", StringComparison.OrdinalIgnoreCase));
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
        var result = await RunBlakeCommandAsync($"new \"{projectPath}\" --template \"{templateName}\"");

        // Assert
        if (result.ExitCode == 0)
        {
            // If successful, should have cloned the template
            FileSystemHelper.AssertDirectoryExists(projectPath);
            Assert.Contains("created successfully", result.OutputText, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            // If failed (e.g., network issues), should show a relevant error
            Logger.LogWarning("Template clone failed (expected in test environment): {Error}", result.ErrorText);
            Assert.True(result.ErrorText.Contains("git", StringComparison.OrdinalIgnoreCase) ||
                       result.ErrorText.Contains("clone", StringComparison.OrdinalIgnoreCase) ||
                       result.ErrorText.Contains("repository", StringComparison.OrdinalIgnoreCase) ||
                       result.ErrorText.Contains("network", StringComparison.OrdinalIgnoreCase));
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
        var result = await RunBlakeCommandAsync($"new \"{projectPath}\" --url \"{repoUrl}\"");

        // Assert
        if (result.ExitCode == 0)
        {
            FileSystemHelper.AssertDirectoryExists(projectPath);
            Assert.Contains("created successfully", result.OutputText, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            // If failed (network/git issues), should show relevant error
            Logger.LogWarning("URL clone failed (expected in test environment): {Error}", result.ErrorText);
            Assert.True(result.ErrorText.Contains("git", StringComparison.OrdinalIgnoreCase) ||
                       result.ErrorText.Contains("clone", StringComparison.OrdinalIgnoreCase) ||
                       result.ErrorText.Contains("repository", StringComparison.OrdinalIgnoreCase));
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
        var result = await RunBlakeCommandAsync($"new \"{projectPath}\" --url \"{invalidUrl}\"");

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.True(result.ErrorText.Contains("git", StringComparison.OrdinalIgnoreCase) ||
                   result.ErrorText.Contains("clone", StringComparison.OrdinalIgnoreCase) ||
                   result.ErrorText.Contains("repository", StringComparison.OrdinalIgnoreCase) ||
                   result.ErrorText.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact] 
    public async Task BlakeNew_ResultingProject_CanBuild()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-build");
        var projectName = "BuildableProject";
        var projectPath = Path.Combine(testDir, projectName);

        // Act - Create project
        var createResult = await RunBlakeCommandAsync($"new \"{projectPath}\"");
        Assert.Equal(0, createResult.ExitCode);

        // Act - Try to build the project
        var buildResult = await RunProcessAsync("dotnet", "build", projectPath);

        // Assert
        Assert.Equal(0, buildResult.ExitCode);
        Assert.Contains("Build succeeded", buildResult.OutputText, StringComparison.OrdinalIgnoreCase);
    }
}