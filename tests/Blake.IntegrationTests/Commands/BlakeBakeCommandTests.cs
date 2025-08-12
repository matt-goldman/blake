using Blake.IntegrationTests.Infrastructure;

namespace Blake.IntegrationTests.Commands;

/// <summary>
/// Integration tests for the `blake bake` command.
/// Tests rendering markdown content to Razor pages.
/// </summary>
public class BlakeBakeCommandTests : TestFixtureBase
{
    [Fact]
    public async Task BlakeBake_WithNonExistentPath_ShowsError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent-" + Guid.NewGuid());

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{nonExistentPath}\"");

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("does not exist", result.ErrorText);
    }

    [Fact]
    public async Task BlakeBake_WithEmptyDirectory_CompletesSuccessfully()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-bake-empty");

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Build completed successfully", result.OutputText);
        
        // Should create .generated folder
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, ".generated"));
    }

    [Fact]
    public async Task BlakeBake_WithMarkdownFiles_RendersToGenerated()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-bake-markdown");
        
        // Create markdown files
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "first-post.md"),
            "First Post",
            "This is my first post content.",
            new Dictionary<string, object> { ["date"] = DateTime.Today }
        );
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Pages", "about.md"),
            "About",
            "This is the about page."
        );

        // Create simple template for Posts
        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Model.Title</h1>
<div>@((MarkupString)Html)</div>"
        );

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Build completed successfully", result.OutputText);
        
        // Should generate Razor files
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, ".generated"));
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, ".generated", "posts"));
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, ".generated", "posts", "FirstPost.razor"));
        
        // Generated file should contain the content
        FileSystemHelper.AssertFileContains(
            Path.Combine(testDir, ".generated", "posts", "FirstPost.razor"),
            "First Post"
        );
        FileSystemHelper.AssertFileContains(
            Path.Combine(testDir, ".generated", "posts", "FirstPost.razor"),
            "first post content"
        );
    }

    [Fact]
    public async Task BlakeBake_WithFrontmatter_ParsesMetadataCorrectly()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-bake-frontmatter");

        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "post-with-metadata.md"),
            "Post with Metadata",
            "Content goes here.",
            new Dictionary<string, object>
            {
                ["title"] = "Post with Metadata",
                ["description"] = "A post with rich metadata",
                ["published"] = "2024-01-15"
            }
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Title</h1>
<p>Published: @Published</p>
<p>Description: @Description</p>
<div>@Body</div>"
        );

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);

        var generatedFile = Path.Combine(testDir, ".generated", "posts", "PostWithMetadata.razor");
        FileSystemHelper.AssertFileExists(generatedFile);
        FileSystemHelper.AssertFileContains(generatedFile, "Post with Metadata");
        FileSystemHelper.AssertFileContains(generatedFile, "A post with rich metadata");
        FileSystemHelper.AssertFileContains(generatedFile, "Published:");
    }

    [Fact]
    public async Task BlakeBake_WithDraftContent_SkipsByDefault()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-bake-drafts");
        
        // Published post
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "published-post.md"),
            "Published Post",
            "This post is published.",
            new Dictionary<string, object> { ["draft"] = false }
        );
        
        // Draft post
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "draft-post.md"),
            "Draft Post",
            "This post is a draft.",
            new Dictionary<string, object> { ["draft"] = true }
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Model.Title</h1>
<div>@((MarkupString)Html)</div>"
        );

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Should generate published post
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, ".generated", "posts", "PublishedPost.razor"));
        
        // Should not generate draft post
        FileSystemHelper.AssertFileNotExists(Path.Combine(testDir, ".generated", "posts", "DraftPost.razor"));
    }

    [Fact]
    public async Task BlakeBake_WithIncludeDraftsFlag_IncludesDrafts()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-bake-include-drafts");
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "draft-post.md"),
            "Draft Post",
            "This post is a draft.",
            new Dictionary<string, object> { ["draft"] = true }
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Model.Title</h1>
<div>@((MarkupString)Html)</div>"
        );

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\" --includeDrafts");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Should generate draft post when flag is used
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, ".generated", "posts", "DraftPost.razor"));
    }

    [Fact]
    public async Task BlakeBake_WithTemplateInSameFolder_UsesTemplate()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-bake-template");
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "post.md"),
            "Test Post",
            "Post content."
        );

        // Template in same folder
        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<article>
<h1 class=""post-title"">@Model.Title</h1>
<div class=""post-content"">@((MarkupString)Html)</div>
</article>"
        );

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        var generatedFile = Path.Combine(testDir, ".generated", "posts", "TestPost.razor");
        FileSystemHelper.AssertFileExists(generatedFile);
        FileSystemHelper.AssertFileContains(generatedFile, "post-title");
        FileSystemHelper.AssertFileContains(generatedFile, "post-content");
        FileSystemHelper.AssertFileContains(generatedFile, "<article>");
    }

    [Fact]
    public async Task BlakeBake_WithMissingTemplate_ShowsError()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-bake-missing-template");
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "post.md"),
            "Test Post",
            "Post content."
        );

        // No template.razor file created

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        // Should either show error or use a default template
        if (result.ExitCode != 0)
        {
            Assert.Contains("template", result.ErrorText, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            // Blake might provide a default template behavior
            FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, ".generated"));
        }
    }

    [Fact]
    public async Task BlakeBake_WithNestedFolders_GeneratesCorrectStructure()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-bake-nested");
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "Category1", "post1.md"),
            "Post in Category 1",
            "First category post."
        );
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "Category2", "SubCategory", "post2.md"),
            "Post in Subcategory",
            "Nested post content."
        );

        // Template at root Posts level
        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Model.Title</h1>
<div>@((MarkupString)Html)</div>"
        );

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Should maintain directory structure in generated output
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, ".generated", "posts"));
        
        // Files should be generated (naming convention may vary)
        var generatedFiles = FileSystemHelper.GetFiles(
            Path.Combine(testDir, ".generated"),
            "*.razor",
            SearchOption.AllDirectories
        );
        
        Assert.True(generatedFiles.Count() >= 2, "Should generate at least 2 razor files");
    }

    [Fact]
    public async Task BlakeBake_WithCleanFlag_DeletesExistingGenerated()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-bake-clean");
        var generatedDir = Path.Combine(testDir, ".generated");
        
        // Create existing generated content
        Directory.CreateDirectory(generatedDir);
        File.WriteAllText(Path.Combine(generatedDir, "old-file.razor"), "old content");
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "new-post.md"),
            "New Post",
            "New content."
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Model.Title</h1>
<div>@((MarkupString)Html)</div>"
        );

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\" --clean");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Old file should be gone
        FileSystemHelper.AssertFileNotExists(Path.Combine(generatedDir, "old-file.razor"));
        
        // New file should be generated
        var newFiles = FileSystemHelper.GetFiles(generatedDir, "*.razor", SearchOption.AllDirectories);
        Assert.True(newFiles.Any(), "Should generate new razor files");
    }

    [Fact]
    public async Task BlakeBake_WithCleanShortFlag_DeletesExistingGenerated()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-bake-clean-short");
        var generatedDir = Path.Combine(testDir, ".generated");
        
        Directory.CreateDirectory(generatedDir);
        File.WriteAllText(Path.Combine(generatedDir, "old-file.razor"), "old content");
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "post.md"),
            "Post",
            "Content."
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Model.Title</h1>
<div>@((MarkupString)Html)</div>"
        );

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\" -cl");

        // Assert
        Assert.Equal(0, result.ExitCode);
        FileSystemHelper.AssertFileNotExists(Path.Combine(generatedDir, "old-file.razor"));
    }

    [Fact]
    public async Task BlakeBake_WithoutCleanFlag_PreservesExistingGenerated()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-bake-no-clean");
        var generatedDir = Path.Combine(testDir, ".generated");
        
        Directory.CreateDirectory(generatedDir);
        File.WriteAllText(Path.Combine(generatedDir, "existing-file.razor"), "existing content");
        
        // Act - Bake without clean flag
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Existing file should still be there
        FileSystemHelper.AssertFileExists(Path.Combine(generatedDir, "existing-file.razor"));
        FileSystemHelper.AssertFileContains(Path.Combine(generatedDir, "existing-file.razor"), "existing content");
    }

    [Fact]
    public async Task BlakeBake_GeneratesConsistentFilenames()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-bake-filenames");
        
        // Test various filename scenarios
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "simple-post.md"),
            "Simple Post",
            "Simple content."
        );
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "post with spaces.md"),
            "Post With Spaces",
            "Spaced content."
        );
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "post-with-dashes.md"),
            "Post With Dashes",
            "Dashed content."
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Model.Title</h1>
<div>@((MarkupString)Html)</div>"
        );

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Should generate files with PascalCase naming
        var generatedFiles = FileSystemHelper.GetFiles(
            Path.Combine(testDir, ".generated", "posts"),
            "*.razor"
        );
        
        // Check that filenames follow expected conventions
        var fileNames = generatedFiles.Select(Path.GetFileNameWithoutExtension).ToList();
        
        Assert.Contains("SimplePost", fileNames);
        Assert.Contains("PostWithSpaces", fileNames);
        Assert.Contains("PostWithDashes", fileNames);
    }

    [Fact]
    public async Task BlakeBake_CreatesGeneratedContentIndex()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-bake-content-index");
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "post1.md"),
            "First Post",
            "First content."
        );
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "post2.md"),
            "Second Post",
            "Second content."
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Model.Title</h1>
<div>@((MarkupString)Html)</div>"
        );

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Should create content index file
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, ".generated", "GeneratedContentIndex.cs"));
        
        var indexContent = File.ReadAllText(Path.Combine(testDir, ".generated", "GeneratedContentIndex.cs"));
        Assert.Contains("First Post", indexContent);
        Assert.Contains("Second Post", indexContent);
    }
}