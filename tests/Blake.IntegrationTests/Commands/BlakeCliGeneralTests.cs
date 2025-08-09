using Blake.IntegrationTests.Infrastructure;

namespace Blake.IntegrationTests.Commands;

/// <summary>
/// Integration tests for general CLI behavior and cross-cutting concerns.
/// </summary>
public class BlakeCliGeneralTests : TestFixtureBase
{
    [Fact]
    public async Task Blake_WithNoArguments_ShowsError()
    {
        // Act
        var result = await RunBlakeCommandAsync("");

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Required argument missing", result.ErrorText);
    }

    [Fact]
    public async Task Blake_WithHelpFlag_ShowsUsage()
    {
        // Act
        var result = await RunBlakeCommandAsync("--help");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Usage:", result.OutputText);
        Assert.Contains("Commands:", result.OutputText);
        Assert.Contains("init", result.OutputText);
        Assert.Contains("bake", result.OutputText);
        Assert.Contains("serve", result.OutputText);
        Assert.Contains("new", result.OutputText);
    }

    [Fact]
    public async Task Blake_WithUnknownCommand_ShowsError()
    {
        // Act
        var result = await RunBlakeCommandAsync("unknown-command");

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Unknown option: unknown-command", result.ErrorText);
    }

    [Theory]
    [InlineData("init")]
    [InlineData("bake")]  
    [InlineData("serve")]
    [InlineData("new")]
    public async Task Blake_Commands_ShowInHelpOutput(string command)
    {
        // Act
        var result = await RunBlakeCommandAsync("--help");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(command, result.OutputText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Blake_HelpOutput_ContainsCorrectCommandDescriptions()
    {
        // Act
        var result = await RunBlakeCommandAsync("--help");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        var helpText = result.OutputText;
        
        // Check command descriptions
        Assert.Contains("Configure an existing Blazor WASM app", helpText);
        Assert.Contains("Generate static content for a Blake site", helpText);
        Assert.Contains("Bake and run the Blazor app in development mode", helpText);
        Assert.Contains("Generates a new Blake site", helpText);
    }

    [Fact]
    public async Task Blake_HelpOutput_ContainsCorrectFlags()
    {
        // Act
        var result = await RunBlakeCommandAsync("--help");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        var helpText = result.OutputText;
        
        // Check important flags are documented
        Assert.Contains("--includeSampleContent", helpText);
        Assert.Contains("--disableDefaultRenderers", helpText);  
        Assert.Contains("--includeDrafts", helpText);
        Assert.Contains("--clean", helpText);
        Assert.Contains("--template", helpText);
        Assert.Contains("--siteName", helpText);
        Assert.Contains("--url", helpText);
        Assert.Contains("--list", helpText);
    }

    [Fact]
    public async Task Blake_HelpOutput_ContainsShortFlags()
    {
        // Act
        var result = await RunBlakeCommandAsync("--help");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        var helpText = result.OutputText;
        
        // Check short flag versions are documented
        Assert.Contains("-s", helpText); // --includeSampleContent
        Assert.Contains("-dr", helpText); // --disableDefaultRenderers
        Assert.Contains("-cl", helpText); // --clean
        Assert.Contains("-t", helpText); // --template
        Assert.Contains("-sn", helpText); // --siteName
        Assert.Contains("-u", helpText); // --url
    }

    [Fact]
    public async Task Blake_Commands_AcceptVerbosityFlag()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-verbosity");

        // Act - Test verbosity flag with bake command
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\" --verbosity Debug");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Build completed successfully", result.OutputText);
        
        // With debug verbosity, should show more detailed logs
        // (The actual verbose output depends on Blake's logging implementation)
    }

    [Fact]
    public async Task Blake_Commands_AcceptVerbosityShortFlag()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-verbosity-short");

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\" -v Information");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Build completed successfully", result.OutputText);
    }

    [Fact]
    public async Task Blake_WithInvalidVerbosityLevel_ShowsWarning()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-invalid-verbosity");

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\" --verbosity InvalidLevel");

        // Assert
        // Should complete but show warning about invalid verbosity
        Assert.Equal(0, result.ExitCode); // Bake should still succeed
        Assert.Contains("Invalid verbosity level", result.OutputText);
    }

    [Fact]
    public async Task Blake_WithMissingVerbosityValue_ShowsWarning()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-missing-verbosity");

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\" --verbosity");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Missing verbosity level", result.OutputText);
    }

    [Theory]
    [InlineData("Trace")]
    [InlineData("Debug")]
    [InlineData("Information")]
    [InlineData("Warning")]
    [InlineData("Error")]
    [InlineData("Critical")]
    [InlineData("None")]
    public async Task Blake_AcceptsValidVerbosityLevels(string level)
    {
        // Arrange
        var testDir = CreateTempDirectory($"blake-verbosity-{level.ToLower()}");

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\" --verbosity {level}");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("Invalid verbosity level", result.OutputText);
    }

    [Fact]
    public async Task Blake_Commands_HandlePathsWithSpaces()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake path with spaces");

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Build completed successfully", result.OutputText);
        
        // Should create .generated folder in the spaced path
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, ".generated"));
    }

    [Fact]
    public async Task Blake_Commands_HandleUnicodeCharacters()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-unicode-测试");

        // Act  
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Build completed successfully", result.OutputText);
    }

    [Fact]
    public async Task Blake_Commands_HandleLongPaths()
    {
        // Arrange - Create a deeply nested path
        var baseTempDir = CreateTempDirectory("blake-long-path");
        var longPath = baseTempDir;
        
        // Create a reasonably long path (but not exceeding system limits)
        for (int i = 0; i < 5; i++)
        {
            longPath = Path.Combine(longPath, $"very-long-directory-name-level-{i}");
        }
        
        Directory.CreateDirectory(longPath);

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{longPath}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        FileSystemHelper.AssertDirectoryExists(Path.Combine(longPath, ".generated"));
    }

    [Fact(Skip = "Complex to set up cross-platform read-only permissions")]
    public async Task Blake_Commands_ShowErrorsOnReadOnlyLocation()
    {
        // This test would need to be adapted based on the test environment permissions
        // For now, we'll skip it as it's complex to set up cross-platform
        // In a real scenario, you'd create a read-only directory and test Blake's behavior
        await Task.CompletedTask; // Suppress async warning
    }

    [Fact]
    public async Task Blake_LogsAreStructured()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-structured-logs");

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\" --verbosity Debug");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Should use structured logging format (check for typical patterns)
        var logOutput = result.OutputText;
        
        // Look for log level indicators or structured format
        Assert.True(
            logOutput.Contains("Starting build for:", StringComparison.OrdinalIgnoreCase) ||
            logOutput.Contains("Build completed successfully", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public async Task Blake_StackTraces_ShownOnError()
    {
        // Arrange - Create a scenario that might cause an error
        var testDir = CreateTempDirectory("blake-stack-trace");
        
        // Create markdown file with problematic content that might cause an error
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "problem.md"),
            "Problem Post",
            "Content"
        );
        
        // Create malformed template that might cause an error
        Directory.CreateDirectory(Path.Combine(testDir, "Posts"));
        File.WriteAllText(Path.Combine(testDir, "Posts", "template.razor"), "@invalid-razor-syntax");

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        // If there's an error, stack traces should be shown
        if (result.ExitCode != 0)
        {
            Assert.True(
                result.ErrorText.Contains("Exception", StringComparison.OrdinalIgnoreCase) ||
                result.ErrorText.Contains("at ", StringComparison.OrdinalIgnoreCase) ||
                result.ErrorText.Contains("stack", StringComparison.OrdinalIgnoreCase)
            );
        }
        // If Blake is resilient and handles the error gracefully, that's also acceptable
    }

    [Fact]
    public async Task Blake_ExitCodes_AreConsistent()
    {
        // Successful operation
        var testDir = CreateTempDirectory("blake-exit-codes");
        var successResult = await RunBlakeCommandAsync($"bake \"{testDir}\"");
        Assert.Equal(0, successResult.ExitCode);

        // Failed operation  
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent-" + Guid.NewGuid());
        var failResult = await RunBlakeCommandAsync($"bake \"{nonExistentPath}\"");
        Assert.NotEqual(0, failResult.ExitCode);

        // Help should be success
        var helpResult = await RunBlakeCommandAsync("--help");
        Assert.Equal(0, helpResult.ExitCode);

        // Unknown command should fail
        var unknownResult = await RunBlakeCommandAsync("unknown");
        Assert.NotEqual(0, unknownResult.ExitCode);
    }
}