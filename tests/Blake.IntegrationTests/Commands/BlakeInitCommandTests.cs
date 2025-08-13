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
        Assert.Contains("No .csproj file found", result.ErrorText);
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
        Assert.Contains("does not exist", result.ErrorText);
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
        Assert.Contains("Initializing Blake", result.OutputText);

        // Should create Blake-specific folders
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, "Posts"));
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, "Pages"));
        
        // Should create .generated folder structure
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, ".generated"));
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
        Assert.Contains("Initializing Blake", result.OutputText);

        // Should create Blake content folders
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, "Posts"));
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, "Pages"));
    }

    [Fact]
    public async Task BlakeInit_WithMultipleCsprojFiles_UsesFirst()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-multiple-csproj");
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, "FirstProject");
        
        // Create a second .csproj file
        File.WriteAllText(Path.Combine(testDir, "SecondProject.csproj"), 
            "<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        // Act
        var result = await RunBlakeCommandAsync($"init \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        // Should use the first .csproj file found
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, "Posts"));
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, "Pages"));
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
        
        // Should create sample content
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, "Posts", "hello-world.md"));
        
        // Should update navigation or layout (check for Blake integration)
        var navFile = Path.Combine(testDir, "Shared", "NavMenu.razor");
        if (File.Exists(navFile))
        {
            FileSystemHelper.AssertFileContains(navFile, "Posts");
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
        
        // Should create sample content
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, "Posts", "hello-world.md"));
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
        
        // Should create directories but not sample files
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, "Posts"));
        FileSystemHelper.AssertFileNotExists(Path.Combine(testDir, "Posts", "hello-world.md"));
    }

    [Fact]
    // TODO: Blake doesn't do this. Perhaps this test should be updated to verify that the starter content is added if the correct flag is used.
    public async Task BlakeInit_CreatesNecessaryContentFolders()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-folders");
        var projectName = "FolderProject";
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);

        // Act
        var result = await RunBlakeCommandAsync($"init \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);

        // Should create Blake content folders
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, "Posts"));
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, "Pages"));
        FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, ".generated"));
    }

    [Fact]
    // TODO: Filenames for sample content are incorrect. Additionally, Blake doesn't create a "Posts" folder by default, the sample content is created in the "Pages" folder. Finally Blake does not return this error message.
    public async Task BlakeInit_DoesNotOverwriteExistingFiles()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-no-overwrite");
        var projectName = "ExistingProject";
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);

        // Create existing content
        var existingPostPath = Path.Combine(testDir, "Posts", "existing-post.md");
        FileSystemHelper.CreateMarkdownFile(existingPostPath, "Existing Post", "This post already exists.");

        var originalContent = File.ReadAllText(existingPostPath);

        // Act
        var result = await RunBlakeCommandAsync($"init \"{testDir}\" --includeSampleContent");

        // Assert
        Assert.Equal(0, result.ExitCode);

        // Should not overwrite existing post
        var currentContent = File.ReadAllText(existingPostPath);
        Assert.Equal(originalContent, currentContent);

        // Should still create other sample content
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, "Posts", "hello-world.md"));
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
        // Arrange
        var testDir = CreateTempDirectory("blake-init-build");
        var projectName = "BuildableInit";
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);

        // Act - Initialize Blake
        var initResult = await RunBlakeCommandAsync($"init \"{testDir}\" --includeSampleContent");
        Assert.Equal(0, initResult.ExitCode);

        // Act - Try to build the project
        var buildResult = await RunProcessAsync("dotnet", "build", testDir);

        // Assert - Project should still be buildable after Blake init
        if (buildResult.ExitCode != 0)
        {
            Logger.LogError("Build failed: {Output}\n{Error}", buildResult.OutputText, buildResult.ErrorText);
        }
        Assert.Equal(0, buildResult.ExitCode);
        Assert.Contains("Build succeeded", buildResult.OutputText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BlakeInit_WithNonBlazorProject_ShowsWarningOrError()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-init-non-blazor");
        var projectName = "ConsoleApp";
        
        // Create a console app project instead of Blazor WASM
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";
        
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(testDir, $"{projectName}.csproj"), csprojContent);
        File.WriteAllText(Path.Combine(testDir, "Program.cs"), "Console.WriteLine(\"Hello World\");");

        // Act
        var result = await RunBlakeCommandAsync($"init \"{testDir}\"");

        // Assert - Should either succeed (Blake is flexible) or show appropriate warning
        if (result.ExitCode == 0)
        {
            // Blake initialized successfully on non-Blazor project
            FileSystemHelper.AssertDirectoryExists(Path.Combine(testDir, "Posts"));
        }
        else
        {
            // Blake detected this isn't a suitable project type
            Assert.Contains("Blazor", result.ErrorText, StringComparison.OrdinalIgnoreCase);
        }
    }
}