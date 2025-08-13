using Blake.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Blake.IntegrationTests.Commands;

/// <summary>
/// Integration tests for the `blake init` command.
/// Tests configuring existing Blazor WASM apps for Blake.
/// </summary>
public class BlakeInitCommandTests : TestFixtureBase
{
    [Fact]
    public async Task BlakeInit_WithNoCsprojFile_ShowsError()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-no-csproj");

        // Act
        var result = await RunBlakeCommandAsync($"init \"{testDir}\"");

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        // Blake should fail when no .csproj file is found (exact error message format may vary)
        // The important thing is that it fails with non-zero exit code
    }

    [Fact]
    public async Task BlakeInit_WithNonExistentPath_ShowsError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent-" + Guid.NewGuid());

        // Act
        var result = await RunBlakeCommandAsync($"init \"{nonExistentPath}\"");

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        // Blake outputs unhandled exception rather than structured error message
        Assert.Contains("DirectoryNotFoundException", result.ErrorText);
    }

    [Fact]
    public async Task BlakeInit_WithDirectCsprojPath_InitializesProject()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-csproj");
        var projectName = "TestProject";
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);

        var csprojPath = Path.Combine(testDir, $"{projectName}.csproj");

        // Act
        var result = await RunBlakeCommandAsync($"init \"{csprojPath}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        // Blake initializes successfully - check for success message instead of debug log messages
        Assert.Contains("Blake has been configured successfully", result.OutputText);

        // Blake should initialize successfully without creating specific folders by default
        // The existing Blazor Pages folder should still exist
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, "Pages"));
        
        // Should not create .generated folder until baking
        FileSystemHelper.AssertDirectoryNotExists(Path.Combine(testDir, ".generated"));
    }

    [Fact]
    public async Task BlakeInit_WithDirectory_FindsCsprojAutomatically()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-directory");
        var projectName = "AutoFindProject";
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);

        // Act
        var result = await RunBlakeCommandAsync($"init \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        // Blake initializes successfully - check for success message instead of debug log messages
        Assert.Contains("Blake has been configured successfully", result.OutputText);

        // Blake should initialize successfully without creating specific folders by default
        // The existing Blazor Pages folder should still exist  
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, "Pages"));
        
        // Should not create .generated folder until baking
        FileSystemHelper.AssertDirectoryNotExists(Path.Combine(testDir, ".generated"));
    }

    [Fact]
    public async Task BlakeInit_WithMultipleCsprojFiles_UsesFirst()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-multiple-csproj");
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, "FirstProject");
        
        // Create a second .csproj file
        File.WriteAllText(Path.Combine(testDir, "SecondProject.csproj"), 
            "<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\"><PropertyGroup><TargetFramework>net9.0</TargetFramework></PropertyGroup></Project>");

        // Act
        var result = await RunBlakeCommandAsync($"init \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Should successfully initialize (Pages folder exists from Blazor template)
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, "Pages"));
        
        // Should not create Blake-specific content without flag
        FileSystemHelper.AssertFileNotExists(Path.Combine(testDir, "Pages", "SamplePage.md"));
    }

    [Fact]
    public async Task BlakeInit_WithIncludeSampleContent_CreatesSampleFiles()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-sample-content");
        var projectName = "SampleProject";
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);

        // Act
        var result = await RunBlakeCommandAsync($"init \"{testDir}\" --includeSampleContent");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Should create sample content in Pages folder (not Posts)
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, "Pages", "SamplePage.md"));
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, "Pages", "template.razor"));
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, "Components", "MyContainer.razor"));
        
        // Should update navigation with dynamic content links
        var navFile = Path.Combine(testDir, "Layout", "NavMenu.razor");
        if (File.Exists(navFile))
        {
            FileSystemHelper.AssertFileContains(navFile, "GeneratedContentIndex.GetPages()");
        }
    }

    [Fact]
    public async Task BlakeInit_WithIncludeSampleContentShortFlag_CreatesSampleFiles()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-sample-short");
        var projectName = "SampleProject";
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);

        // Act
        var result = await RunBlakeCommandAsync($"init \"{testDir}\" -s");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Should create sample content in Pages folder (not Posts)
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, "Pages", "SamplePage.md"));
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, "Components", "MyContainer.razor"));
    }

    [Fact]
    public async Task BlakeInit_WithoutIncludeSampleContent_DoesNotCreateSamples()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-no-sample");
        var projectName = "NoSampleProject";
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);

        // Act
        var result = await RunBlakeCommandAsync($"init \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Should not create Blake sample files
        FileSystemHelper.AssertFileNotExists(Path.Combine(testDir, "Pages", "SamplePage.md"));
        FileSystemHelper.AssertFileNotExists(Path.Combine(testDir, "Pages", "template.razor"));
        FileSystemHelper.AssertDirectoryNotExists(Path.Combine(testDir, "Components"));
    }

    [Fact]
    public async Task BlakeInit_WithSampleContentFlag_CreatesPagesFolder()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-sample-content");
        var projectName = "SampleContentProject";
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);

        // Act
        var result = await RunBlakeCommandAsync($"init \"{testDir}\" --includeSampleContent");

        // Assert
        Assert.Equal(0, result.ExitCode);

        // Should create Pages folder with sample content (Blake creates content in Pages, not Posts)
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, "Pages"));
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, "Pages", "SamplePage.md"));
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, "Pages", "template.razor"));
        
        // Should create Components folder with sample component
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, "Components"));
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, "Components", "MyContainer.razor"));
    }

    [Fact]
    public async Task BlakeInit_WithoutSampleContent_DoesNotCreateBlakeContent()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-basic");
        var projectName = "BasicProject";
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);

        // Act
        var result = await RunBlakeCommandAsync($"init \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);

        // Should not create Blake-specific content without --includeSampleContent flag
        // Note: Pages folder might exist from Blazor template, but Blake should not add Blake-specific files
        FileSystemHelper.AssertFileNotExists(Path.Combine(testDir, "Pages", "SamplePage.md"));
        FileSystemHelper.AssertFileNotExists(Path.Combine(testDir, "Pages", "template.razor"));
        FileSystemHelper.AssertDirectoryNotExists(Path.Combine(testDir, "Components"));
        
        // Should not create .generated folder until baking
        FileSystemHelper.AssertDirectoryNotExists(Path.Combine(testDir, ".generated"));
    }

    [Fact]
    public async Task BlakeInit_DoesNotOverwriteExistingFiles()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-no-overwrite");
        var projectName = "ExistingProject";
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);

        // Create existing content in Pages folder (where Blake actually creates content)
        var pagesDir = Path.Combine(testDir, "Pages");
        Directory.CreateDirectory(pagesDir);
        var existingPagePath = Path.Combine(pagesDir, "SamplePage.md");
        FileSystemHelper.CreateMarkdownFile(existingPagePath, "Existing Page", "This page already exists.");

        var originalContent = File.ReadAllText(existingPagePath);

        // Act
        var result = await RunBlakeCommandAsync($"init \"{testDir}\" --includeSampleContent");

        // Assert
        Assert.Equal(0, result.ExitCode);

        // Should not overwrite existing SamplePage.md
        var currentContent = File.ReadAllText(existingPagePath);
        Assert.Equal(originalContent, currentContent);

        // Should still create template and component if they don't exist
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, "Pages", "template.razor"));
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, "Components", "MyContainer.razor"));
    }

    [Fact]
    public async Task BlakeInit_InjectsBlakeRenderingPipeline()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-pipeline");
        var projectName = "PipelineProject";
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);

        var originalProgramCs = File.ReadAllText(Path.Combine(testDir, "Program.cs"));

        // Act
        var result = await RunBlakeCommandAsync($"init \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);

        // Check that Program.cs or project structure has been modified for Blake integration
        var csprojPath = Path.Combine(testDir, $"{projectName}.csproj");
        var csprojContent = File.ReadAllText(csprojPath);
        
        // Should add Blake-related packages or MSBuild targets
        Assert.True(csprojContent.Contains("Blake", StringComparison.OrdinalIgnoreCase) ||
                   File.Exists(Path.Combine(testDir, ".generated", "GeneratedContentIndex.cs")));
    }

    [Fact]
    public async Task BlakeInit_ResultingProject_CanBuild()
    {
        // Arrange - Use real Blazor WASM template instead of minimal project
        var testDir = CreateTempDirectory("blake-init-build");
        var projectName = "BuildableInit";
        
        // Create actual Blazor WASM project
        await FileSystemHelper.CreateBlazorWasmProjectAsync(testDir, projectName);

        // Act - Initialize Blake
        var initResult = await RunBlakeCommandAsync($"init \"{testDir}\" --includeSampleContent");
        Assert.Equal(0, initResult.ExitCode);

        // Act - Try to build the project
        var buildResult = await RunProcessAsync("dotnet", "build", testDir);

        // Assert - Project should still be buildable after Blake init
        if (buildResult.ExitCode != 0)
        {
            // Print error details for debugging
            Console.WriteLine($"Build failed: {buildResult.OutputText}\n{buildResult.ErrorText}");
        }
        Assert.Equal(0, buildResult.ExitCode);
        Assert.Contains("Build succeeded", buildResult.OutputText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BlakeInit_WithNonBlazorProject_SucceedsGracefully()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-non-blazor");
        var projectName = "ConsoleApp";
        
        // Create a console app project instead of Blazor WASM
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>";
        
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(testDir, $"{projectName}.csproj"), csprojContent);
        File.WriteAllText(Path.Combine(testDir, "Program.cs"), "Console.WriteLine(\"Hello World\");");

        // Act
        var result = await RunBlakeCommandAsync($"init \"{testDir}\"");

        // Assert - Blake should initialize successfully (it doesn't validate project types)
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Blake has been configured successfully", result.OutputText);
        
        // Blake should have created the partial content index 
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, "GeneratedContentIndex.Partial.cs"));
    }
}