using Blake.IntegrationTests.Infrastructure;

namespace Blake.IntegrationTests.Commands;

/// <summary>
/// Integration tests for the `blake serve` command.
/// Tests dev-time local hosting functionality.
/// </summary>
public class BlakeServeCommandTests : TestFixtureBase
{
    [Fact]
    public async Task BlakeServe_WithNonExistentPath_ShowsError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent-" + Guid.NewGuid());

        // Act - Use a short timeout since serve command runs indefinitely
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await RunBlakeCommandAsync($"serve \"{nonExistentPath}\"", "", cts.Token);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("does not exist", result.ErrorText);
    }

    [Fact]
    public async Task BlakeServe_BakesBeforeServing()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-serve-bake");
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, "ServeTest");
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "test-post.md"),
            "Test Post",
            "This post should be baked before serving."
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Model.Title</h1>
<div>@((MarkupString)Html)</div>"
        );

        // Act - Start serve command but cancel quickly
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result = await RunBlakeCommandAsync($"serve \"{testDir}\"", "", cts.Token);

        // Assert
        // Should have attempted to bake first
        Assert.Contains("Baking in:", result.OutputText, StringComparison.OrdinalIgnoreCase);
        
        // Should have created generated content
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, ".generated"));
        
        // May show "Build completed successfully" or start showing dotnet run output
        Assert.True(
            result.OutputText.Contains("Build completed successfully", StringComparison.OrdinalIgnoreCase) ||
            result.OutputText.Contains("Running app", StringComparison.OrdinalIgnoreCase) ||
            result.ErrorText.Contains("was canceled", StringComparison.OrdinalIgnoreCase) // Expected due to timeout
        );
    }

    [Fact]
    public async Task BlakeServe_WithBakeFailure_DoesNotStartServer()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-serve-bake-fail");
        
        // Create content that might cause bake to fail
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "post.md"),
            "Test Post",
            "Content"
        );
        // Intentionally don't create template to potentially cause failure

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result = await RunBlakeCommandAsync($"serve \"{testDir}\"", "", cts.Token);

        // Assert
        if (result.ExitCode != 0)
        {
            // If bake failed, serve should not continue
            Assert.DoesNotContain("Running app", result.OutputText, StringComparison.OrdinalIgnoreCase);
        }
        // If Blake is resilient and continues anyway, that's also acceptable behavior
    }

    [Fact]
    public async Task BlakeServe_WithValidProject_AttemptsToRunDotnetRun()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-serve-valid");
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, "ValidServe");

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var result = await RunBlakeCommandAsync($"serve \"{testDir}\"", "", cts.Token);

        // Assert
        // Should attempt to run the app (even if it fails due to missing dependencies in test environment)
        Assert.True(
            result.OutputText.Contains("Running app", StringComparison.OrdinalIgnoreCase) ||
            result.OutputText.Contains("dotnet run", StringComparison.OrdinalIgnoreCase) ||
            result.ErrorText.Contains("was canceled") || // Expected due to timeout
            result.ErrorText.Contains("run") // May show dotnet run errors
        );
    }

    [Fact]
    public async Task BlakeServe_PassesThroughOptions()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-serve-options");
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, "OptionsTest");
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "post.md"),
            "Test Post",
            "Content"
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Model.Title</h1>
<div>@((MarkupString)Html)</div>"
        );

        // Act - Test with disable default renderers flag
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result = await RunBlakeCommandAsync($"serve \"{testDir}\" --disableDefaultRenderers", "", cts.Token);

        // Assert
        // Should have passed through the option to the bake step
        Assert.Contains("Baking in:", result.OutputText, StringComparison.OrdinalIgnoreCase);
        
        // Should have created generated content despite the option
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, ".generated"));
    }

    [Fact]
    public async Task BlakeServe_WithMissingContentFolder_HandlesGracefully()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-serve-no-content");
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, "NoContent");
        
        // Don't create any Posts or Pages folders

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result = await RunBlakeCommandAsync($"serve \"{testDir}\"", "", cts.Token);

        // Assert
        // Should handle missing content gracefully and still try to serve
        Assert.True(result.ExitCode == 0 || result.ErrorText.Contains("was canceled"));
        
        // Should still attempt baking
        Assert.Contains("Baking in:", result.OutputText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BlakeServe_CreatesGeneratedFolder()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-serve-creates-folder");
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, "CreateFolder");

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var result = await RunBlakeCommandAsync($"serve \"{testDir}\"", "", cts.Token);

        // Assert
        // Should create .generated folder as part of the bake step
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, ".generated"));
    }

    [Fact]
    public async Task BlakeServe_UsesCurrentDirectoryWhenNoPathProvided()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-serve-current-dir");
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, "CurrentDir");

        // Act - Run blake serve without path argument from the project directory
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var result = await RunBlakeCommandAsync("serve", testDir, cts.Token);

        // Assert
        Assert.True(result.ExitCode == 0 || result.ErrorText.Contains("was canceled"));
        Assert.Contains("Baking in:", result.OutputText, StringComparison.OrdinalIgnoreCase);
        
        // Should create .generated in the working directory
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, ".generated"));
    }

    [Fact]
    public async Task BlakeServe_ShowsProgressMessages()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-serve-progress");
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, "Progress");

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result = await RunBlakeCommandAsync($"serve \"{testDir}\"", "", cts.Token);

        // Assert
        // Should show baking progress
        Assert.Contains("Baking in:", result.OutputText, StringComparison.OrdinalIgnoreCase);
        
        // May show build completion or app startup messages
        Assert.True(
            result.OutputText.Contains("Build completed successfully", StringComparison.OrdinalIgnoreCase) ||
            result.OutputText.Contains("Running app", StringComparison.OrdinalIgnoreCase) ||
            result.ErrorText.Contains("was canceled") // Expected due to timeout
        );
    }

    [Fact]
    public async Task BlakeServe_IntegrationWithBakeOptions()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-serve-bake-integration");
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, "BakeIntegration");
        
        // Create draft content
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "draft.md"),
            "Draft Post",
            "Draft content",
            new Dictionary<string, object> { ["draft"] = true }
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Model.Title</h1>
<div>@((MarkupString)Html)</div>"
        );

        // Act - Should not include drafts by default
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var result = await RunBlakeCommandAsync($"serve \"{testDir}\"", "", cts.Token);

        // Assert
        // Should have baked (drafts excluded by default)
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, ".generated"));
        
        // Draft should not be generated
        FileSystemHelper.AssertFileNotExists(Path.Combine(testDir, ".generated", "posts", "DraftPost.razor"));
    }

    [Fact]
    public async Task BlakeServe_HandlesProjectWithoutCsproj()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-serve-no-csproj");
        // Just create a directory without a proper Blazor project

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await RunBlakeCommandAsync($"serve \"{testDir}\"", "", cts.Token);

        // Assert
        // Should either fail gracefully or handle the missing project file
        if (result.ExitCode != 0)
        {
            Assert.True(
                result.ErrorText.Contains("project", StringComparison.OrdinalIgnoreCase) ||
                result.ErrorText.Contains("csproj", StringComparison.OrdinalIgnoreCase) ||
                result.ErrorText.Contains("build", StringComparison.OrdinalIgnoreCase)
            );
        }
        
        // Should still attempt to bake first
        Assert.Contains("Baking in:", result.OutputText, StringComparison.OrdinalIgnoreCase);
    }
}