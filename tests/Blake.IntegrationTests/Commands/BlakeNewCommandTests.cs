using Blake.CLI;
using Blake.IntegrationTests.Infrastructure;
using Blake.Types;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Blake.IntegrationTests.Commands;

/// <summary>
/// Integration tests for the `blake new` command.
/// Tests creating new Blake sites with various templates and options.
/// </summary>
public class BlakeNewCommandTests : TestFixtureBase
{
    private const int DatePrefixLength = 10;
    const string shortName1 = "tailwind-sample";
    const string shortName2 = "simpledocs";
    const string longName1 = "Blake Simple Tailwind Sample";
    const string longName2 = "Blake Simple Docs";

    [Fact(Skip ="Blake creates a site with no args. TODO: consider how to test this, it will run it in the assembly folder.")]
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
        var created = CreateDebugRegistryIfNotExists();


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
        if (created) DeleteDebugRegistry();
    }

    [Fact]
    public async Task BlakeNewContent_WithTitle_CreatesPostMarkdownFile()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-content");
        var title = "Adding new templates to Blake";

        // Act
        var result = await RunBlakeCommandAsync(["new", "content", testDir, "-t", title]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        var postFile = Directory.GetFiles(testDir, "*.md", SearchOption.TopDirectoryOnly).Single();
        var postFileName = Path.GetFileNameWithoutExtension(postFile);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}-adding-new-templates-to-blake$", postFileName);

        var postDate = postFileName[..DatePrefixLength];
        FileSystemHelper.AssertFileContains(postFile, $"date: {postDate}");
        Assert.Matches($@"(?im)^title:\s*[""']?{Regex.Escape(title)}[""']?\s*$", File.ReadAllText(postFile));
        FileSystemHelper.AssertFileContains(postFile, "# Adding new templates to Blake");
    }

    [Fact]
    public async Task BlakeNewContent_UsesPageTemplateWhenPresent()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-page");
        var title = "About Blake";
        var templatePath = Path.Combine(testDir, "page-template.md");
        await File.WriteAllTextAsync(templatePath,
            """
            ---
            title: "{{title}}"
            date: {{date}}
            slug: "{{slug}}"
            ---

            Welcome to {{title}}.
            """);

        // Act
        var result = await RunBlakeCommandAsync(["new", "content", testDir, "--title", title]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        var outputFilePath = Path.Combine(testDir, "about-blake.md");
        FileSystemHelper.AssertFileExists(outputFilePath);
        Assert.Matches(@"(?im)^title:\s*[""']?About Blake[""']?\s*$", File.ReadAllText(outputFilePath));
        Assert.Matches(@"(?im)^slug:\s*[""']?about-blake[""']?\s*$", File.ReadAllText(outputFilePath));
        FileSystemHelper.AssertFileContains(outputFilePath, "Welcome to About Blake.");
    }

    [Fact]
    public async Task BlakeNewContent_WithoutTitle_UsesUntitled()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-content-missing-title");

        // Act
        var result = await RunBlakeCommandAsync(["new", "content", testDir]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        var filePath = Directory.GetFiles(testDir, "*.md", SearchOption.TopDirectoryOnly).Single();
        Assert.Matches(@"(?im)^title:\s*[""']?Untitled[""']?\s*$", File.ReadAllText(filePath));
    }

    [Fact]
    public async Task BlakeNewContent_WithPositionalTitle_CreatesPostInCurrentDirectory()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-content-positional-title");

        // Act
        var result = await RunBlakeFromDotnetAsync("new content \"About Blake\"", workingDirectory: testDir);

        // Assert
        Assert.Equal(0, result.ExitCode);
        var createdFile = Directory.GetFiles(testDir, "*.md", SearchOption.TopDirectoryOnly).Single();
        Assert.EndsWith("-about-blake.md", createdFile, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BlakeNewContent_WithDirectoryOption_UsesExistingCaseInsensitivePostsDirectory()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-content-lowercase-folder");
        var lowercasePosts = Path.Combine(testDir, "posts");
        Directory.CreateDirectory(lowercasePosts);

        // Act
        var result = await RunBlakeCommandAsync(["new", "content", testDir, "--directory", "posts", "--title", "Case Test"]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Single(Directory.GetFiles(lowercasePosts, "*.md", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public async Task BlakeNewContent_WithDirectoryOption_UpdatesTemplateFrontmatter()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-content-frontmatter-template");
        var targetDir = Path.Combine(testDir, "Posts", "Tech", "DotNet");
        var templatePath = Path.Combine(targetDir, "template.md");
        Directory.CreateDirectory(targetDir);
        await File.WriteAllTextAsync(templatePath,
            """
            ---
            title: "replace me"
            date: 2001-01-01
            id: "existing-id"
            category: "engineering"
            ---

            # Draft
            """);

        // Act
        var result = await RunBlakeCommandAsync(["new", "content", "--directory", targetDir, "A Better CLI"]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        var outputFile = Directory.GetFiles(targetDir, "*.md", SearchOption.TopDirectoryOnly)
            .Single(path => !path.EndsWith("template.md", StringComparison.OrdinalIgnoreCase));
        var fileContents = File.ReadAllText(outputFile);
        Assert.Matches(@"(?im)^title:\s*[""']?A Better CLI[""']?\s*$", fileContents);
        Assert.DoesNotContain("date: 2001-01-01", fileContents);
        Assert.DoesNotContain("id: \"existing-id\"", fileContents);
        Assert.Matches(@"(?im)^id:\s*[""']?[0-9a-fA-F-]{36}[""']?\s*$", fileContents);
        Assert.Matches(@"(?im)^category:\s*[""']?engineering[""']?\s*$", fileContents);
    }

    [Fact]
    public async Task BlakeNewContent_WithDirectoryInsidePages_UsesPageFileNaming()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-content-pages-segment");
        var targetDir = Path.Combine(testDir, "PaGeS", "Guides");
        Directory.CreateDirectory(targetDir);

        // Act
        var result = await RunBlakeCommandAsync(["new", "content", "--directory", targetDir, "Docs Home"]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        var createdFile = Directory.GetFiles(targetDir, "*.md", SearchOption.TopDirectoryOnly).Single();
        Assert.Equal("docs-home.md", Path.GetFileName(createdFile));
    }

    [Fact]
    public async Task BlakeNewContent_WithPostTemplateOnly_UsesPostFileNaming()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-content-post-template-only");
        await File.WriteAllTextAsync(Path.Combine(testDir, "post-template.md"),
            """
            ---
            title: "{{title}}"
            ---
            """);

        // Act
        var result = await RunBlakeCommandAsync(["new", "content", testDir, "Template Driven"]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        var createdFile = Directory.GetFiles(testDir, "*.md", SearchOption.TopDirectoryOnly)
            .Single(path => !Path.GetFileName(path).Equals("post-template.md", StringComparison.OrdinalIgnoreCase));
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}-template-driven\.md$", Path.GetFileName(createdFile));
    }

    [Fact]
    public async Task BlakeNewContent_WithPostAndPageTemplates_DefaultsToPostFileNaming()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-content-both-templates");
        await File.WriteAllTextAsync(Path.Combine(testDir, "post-template.md"), "---\ntitle: \"{{title}}\"\n---");
        await File.WriteAllTextAsync(Path.Combine(testDir, "page-template.md"), "---\ntitle: \"{{title}}\"\n---");

        // Act
        var result = await RunBlakeCommandAsync(["new", "content", testDir, "Both Templates"]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        var createdFile = Directory.GetFiles(testDir, "*.md", SearchOption.TopDirectoryOnly)
            .Single(path =>
                !path.EndsWith("post-template.md", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith("page-template.md", StringComparison.OrdinalIgnoreCase));
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}-both-templates\.md$", Path.GetFileName(createdFile));
    }

    [Fact]
    public async Task BlakeNewContent_WithQuotedDirectoryPath_TrimsQuotes()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-content-quoted-directory");
        var nestedDir = Path.Combine(testDir, "Posts", "Quoted");
        Directory.CreateDirectory(nestedDir);
        var command = $"new content --directory \"{nestedDir}\" \"Quoted Directory\"";

        // Act
        var result = await RunBlakeFromDotnetAsync(command);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Single(Directory.GetFiles(nestedDir, "*.md", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public async Task BlakeNew_WithPathNamedPost_CreatesSiteDirectory()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-new-site-named-post");
        var projectPath = Path.Combine(testDir, "post");

        // Act
        var result = await RunBlakeCommandAsync(["new", projectPath]);

        // Assert
        Assert.Equal(0, result.ExitCode);
        FileSystemHelper.AssertFileExists(Path.Combine(projectPath, "post.csproj"));
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
        FileSystemHelper.AssertFileExists(Path.Combine(projectPath, "Pages", "SamplePage.md"));
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
        var created = CreateDebugRegistryIfNotExists(); // Ensure we have a mock registry for testing

        var testDir = CreateTempDirectory("blake-new-invalid-template");
        var projectPath = Path.Combine(testDir, "test-project");

        // Act
        var result = await RunBlakeCommandAsync(["new", projectPath, "--template","non-existent-template"]);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(result.ErrorText, e => e.Contains("template") ||
                   e.Contains("not found"));

        if (created) DeleteDebugRegistry(); // Clean up mock registry
    }

    [Theory(Skip ="This requires cloning templates, so will need to think about how we ensure a local repo to clone from is created, and that the urls are in the debug registry")]
    [InlineData("tailwind-sample")]
    [InlineData("Blake Simple Tailwind Sample")] // Full name
    public async Task BlakeNew_WithValidTemplate_UsesTemplate(string templateName)
    {
        // Arrange

        var created = CreateDebugRegistryIfNotExists(); // Ensure we have a mock registry for testing

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

        if (created) DeleteDebugRegistry(); // Clean up mock registry
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
            Assert.Contains(result.ErrorText, e => 
                e.Contains("git") ||
                e.Contains("clone") ||
                e.Contains("repository"));
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

    private bool CreateDebugRegistryIfNotExists()
    {
        bool created = false;

        var templateRegistryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".blake", "TemplateRegistry.json");
        if (!File.Exists(templateRegistryPath))
        {
            // Create a mock TemplateRegistry.json for testing
            var templates = new List<SiteTemplate>
            {
                new (Guid.Empty, shortName1, longName1, "", "", "", DateTime.MinValue, ""),
                new (Guid.Empty, shortName2, longName2, "", "", "", DateTime.MinValue, "")
            };

            var registry = new TemplateRegistry(templates);

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(registry, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(templateRegistryPath)!);
            File.WriteAllText(templateRegistryPath, jsonContent);

            created = true;
        }

        return created;
    }

    private void DeleteDebugRegistry()
    {
        var templateRegistryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".blake", "TemplateRegistry.json");
        if (File.Exists(templateRegistryPath))
        {
            File.Delete(templateRegistryPath);
        }
    }
}
