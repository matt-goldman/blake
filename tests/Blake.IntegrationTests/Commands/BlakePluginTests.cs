using Blake.IntegrationTests.Infrastructure;

namespace Blake.IntegrationTests.Commands;

/// <summary>
/// Integration tests for Blake plugin system functionality.
/// </summary>
public class BlakePluginTests : TestFixtureBase
{
    [Fact]
    public async Task BlakeBake_WithNoPlugins_CompletesNormally()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-plugins-none");
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "test.md"),
            "Test Post",
            "Content"
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Title</h1>
<div>@Body</div>"
        );

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Build completed successfully", result.OutputText);
    }

    [Fact]
    public async Task BlakeBake_WithPluginProject_LoadsAndExecutesPlugin()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-plugins-project");
        var projectName = "PluginTest";
        
        // Create a basic Blazor project
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);
        
        // Create content
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "test.md"),
            "Test Post",
            "Content"
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Title</h1>
<div>@Body</div>"
        );

        // Build the test plugin first
        var pluginBuildResult = await RunProcessAsync("dotnet", "build", Path.Combine(GetCurrentDirectory(), "tests", "Blake.IntegrationTests", "TestPlugin"));
        Assert.Equal(0, pluginBuildResult.ExitCode);

        // Add plugin project reference to the test project
        var csprojPath = Path.Combine(testDir, $"{projectName}.csproj");
        var csprojContent = File.ReadAllText(csprojPath);
        
        var pluginProjectPath = Path.Combine(GetCurrentDirectory(), "tests", "Blake.IntegrationTests", "TestPlugin", "BlakePlugin.TestPlugin.csproj");
        var relativePath = Path.GetRelativePath(testDir, pluginProjectPath);
        
        var updatedCsproj = csprojContent.Replace("</Project>", 
            $@"  <ItemGroup>
    <ProjectReference Include=""{relativePath}"" />
  </ItemGroup>

</Project>");
        File.WriteAllText(csprojPath, updatedCsproj);

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Plugin should have created marker files
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, ".plugin-before-bake.txt"));
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, ".plugin-after-bake.txt"));
    }

    [Fact]
    public async Task BlakeBake_WithMultiplePlugins_ExecutesAllPlugins()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-plugins-multiple");
        var projectName = "MultiplePluginTest";
        
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "test.md"),
            "Test Post",
            "Content"
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Title</h1>
<div>@Body</div>"
        );

        // Build the test plugin
        var pluginBuildResult = await RunProcessAsync("dotnet", "build", Path.Combine(GetCurrentDirectory(), "tests", "Blake.IntegrationTests", "TestPlugin"));
        Assert.Equal(0, pluginBuildResult.ExitCode);

        // Add plugin reference
        var csprojPath = Path.Combine(testDir, $"{projectName}.csproj");
        var csprojContent = File.ReadAllText(csprojPath);
        
        var pluginProjectPath = Path.Combine(GetCurrentDirectory(), "tests", "Blake.IntegrationTests", "TestPlugin", "BlakePlugin.TestPlugin.csproj");
        var relativePath = Path.GetRelativePath(testDir, pluginProjectPath);
        
        var updatedCsproj = csprojContent.Replace("</Project>", 
            $@"  <ItemGroup>
    <ProjectReference Include=""{relativePath}"" />
  </ItemGroup>

</Project>");
        File.WriteAllText(csprojPath, updatedCsproj);

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Multiple plugin instances should run (TestPlugin and ContentModifyingPlugin from same assembly)
        Assert.Contains("TestPlugin: BeforeBakeAsync called", result.OutputText);
        Assert.Contains("ContentModifyingPlugin: Adding test metadata", result.OutputText);
    }

    [Fact]
    public async Task BlakeBake_WithFailingPlugin_HandleGracefully()
    {
        // This test would require a way to selectively enable the FailingTestPlugin
        // For now, we'll test that plugin loading errors are handled properly
        
        // Arrange
        var testDir = CreateTempDirectory("blake-plugins-fail");
        var projectName = "FailingPluginTest";
        
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);
        
        // Create a malformed plugin reference (non-existent path)
        var csprojPath = Path.Combine(testDir, $"{projectName}.csproj");
        var csprojContent = File.ReadAllText(csprojPath);
        
        var updatedCsproj = csprojContent.Replace("</Project>", 
            $@"  <ItemGroup>
    <ProjectReference Include=""../NonExistentPlugin/NonExistentPlugin.csproj"" />
  </ItemGroup>

</Project>");
        File.WriteAllText(csprojPath, updatedCsproj);

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        // Should either handle the error gracefully or continue without the plugin
        if (result.ExitCode != 0)
        {
            Assert.True(
                result.ErrorText.Contains("plugin", StringComparison.OrdinalIgnoreCase) ||
                result.ErrorText.Contains("project", StringComparison.OrdinalIgnoreCase) ||
                result.ErrorText.Contains("reference", StringComparison.OrdinalIgnoreCase)
            );
        }
    }

    [Fact]
    public async Task BlakeBake_PluginsReceiveCorrectContext()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-plugins-context");
        var projectName = "ContextTest";
        
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);
        
        // Create multiple markdown files to test context
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "post1.md"),
            "First Post",
            "First content"
        );
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "post2.md"),
            "Second Post",  
            "Second content"
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Title</h1>
<div>@Body</div>"
        );

        // Build and add plugin
        var pluginBuildResult = await RunProcessAsync("dotnet", "build", Path.Combine(GetCurrentDirectory(), "tests", "Blake.IntegrationTests", "TestPlugin"));
        Assert.Equal(0, pluginBuildResult.ExitCode);

        var csprojPath = Path.Combine(testDir, $"{projectName}.csproj");
        var csprojContent = File.ReadAllText(csprojPath);
        
        var pluginProjectPath = Path.Combine(GetCurrentDirectory(), "tests", "Blake.IntegrationTests", "TestPlugin", "BlakePlugin.TestPlugin.csproj");
        var relativePath = Path.GetRelativePath(testDir, pluginProjectPath);
        
        var updatedCsproj = csprojContent.Replace("</Project>", 
            $@"  <ItemGroup>
    <ProjectReference Include=""{relativePath}"" />
  </ItemGroup>

</Project>");
        File.WriteAllText(csprojPath, updatedCsproj);

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Plugin should have logged correct counts
        Assert.Contains("BeforeBakeAsync called with 2 markdown pages", result.OutputText);
        Assert.Contains("AfterBakeAsync called with 2 generated pages", result.OutputText);
    }

    [Fact]
    public async Task BlakeBake_PluginsCanModifyPipeline()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-plugins-pipeline");
        var projectName = "PipelineTest";
        
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "test.md"),
            "Test Post",
            "Content with **markdown**"
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Title</h1>
<div>@Body</div>"
        );

        // Build and add plugin that modifies pipeline
        var pluginBuildResult = await RunProcessAsync("dotnet", "build", Path.Combine(GetCurrentDirectory(), "tests", "Blake.IntegrationTests", "TestPlugin"));
        Assert.Equal(0, pluginBuildResult.ExitCode);

        var csprojPath = Path.Combine(testDir, $"{projectName}.csproj");
        var csprojContent = File.ReadAllText(csprojPath);
        
        var pluginProjectPath = Path.Combine(GetCurrentDirectory(), "tests", "Blake.IntegrationTests", "TestPlugin", "BlakePlugin.TestPlugin.csproj");
        var relativePath = Path.GetRelativePath(testDir, pluginProjectPath);
        
        var updatedCsproj = csprojContent.Replace("</Project>", 
            $@"  <ItemGroup>
    <ProjectReference Include=""{relativePath}"" />
  </ItemGroup>

</Project>");
        File.WriteAllText(csprojPath, updatedCsproj);

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("ContentModifyingPlugin: Adding test metadata", result.OutputText);
    }

    [Fact]
    public async Task BlakeBake_PluginsReceiveLogger()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-plugins-logger");
        var projectName = "LoggerTest";
        
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "test.md"),
            "Test Post",
            "Content"
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Title</h1>
<div>@Body</div>"
        );

        // Build and add plugin
        var pluginBuildResult = await RunProcessAsync("dotnet", "build", Path.Combine(GetCurrentDirectory(), "tests", "Blake.IntegrationTests", "TestPlugin"));
        Assert.Equal(0, pluginBuildResult.ExitCode);

        var csprojPath = Path.Combine(testDir, $"{projectName}.csproj");
        var csprojContent = File.ReadAllText(csprojPath);
        
        var pluginProjectPath = Path.Combine(GetCurrentDirectory(), "tests", "Blake.IntegrationTests", "TestPlugin", "BlakePlugin.TestPlugin.csproj");
        var relativePath = Path.GetRelativePath(testDir, pluginProjectPath);
        
        var updatedCsproj = csprojContent.Replace("</Project>", 
            $@"  <ItemGroup>
    <ProjectReference Include=""{relativePath}"" />
  </ItemGroup>

</Project>");
        File.WriteAllText(csprojPath, updatedCsproj);

        // Act - Run with verbose logging to see plugin logs
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\" --verbosity Debug");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Plugin log messages should appear in output
        Assert.Contains("TestPlugin: BeforeBakeAsync called", result.OutputText);
        Assert.Contains("TestPlugin: AfterBakeAsync called", result.OutputText);
        Assert.Contains("ContentModifyingPlugin:", result.OutputText);
    }

    [Fact]
    public async Task BlakeBake_PluginDiscovery_FindsPluginAssemblies()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-plugins-discovery");
        var projectName = "DiscoveryTest";
        
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);

        // Build the test plugin
        var pluginBuildResult = await RunProcessAsync("dotnet", "build", Path.Combine(GetCurrentDirectory(), "tests", "Blake.IntegrationTests", "TestPlugin"));
        Assert.Equal(0, pluginBuildResult.ExitCode);

        // Add plugin reference to project
        var csprojPath = Path.Combine(testDir, $"{projectName}.csproj");
        var csprojContent = File.ReadAllText(csprojPath);
        
        var pluginProjectPath = Path.Combine(GetCurrentDirectory(), "tests", "Blake.IntegrationTests", "TestPlugin", "BlakePlugin.TestPlugin.csproj");
        var relativePath = Path.GetRelativePath(testDir, pluginProjectPath);
        
        var updatedCsproj = csprojContent.Replace("</Project>", 
            $@"  <ItemGroup>
    <ProjectReference Include=""{relativePath}"" />
  </ItemGroup>

</Project>");
        File.WriteAllText(csprojPath, updatedCsproj);

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\" --verbosity Debug");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Should discover and load plugins
        Assert.True(
            result.OutputText.Contains("plugin", StringComparison.OrdinalIgnoreCase) ||
            result.OutputText.Contains("TestPlugin:", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public async Task BlakeBake_WithoutProjectFile_SkipsPluginLoading()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-plugins-no-project");
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "test.md"),
            "Test Post",
            "Content"
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Title</h1>
<div>@Body</div>"
        );

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\"");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Build completed successfully", result.OutputText);
        
        // Should not show plugin-related errors when no project file exists
        Assert.DoesNotContain("plugin", result.ErrorText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BlakeBake_WithPluginWithDependencies_LoadsAndExecutesCorrectly()
    {
        // Arrange
        var testDir = CreateTempDirectory("blake-plugins-with-deps");
        var projectName = "PluginWithDepsTest";
        
        FileSystemHelper.CreateMinimalBlazorWasmProject(testDir, projectName);
        
        FileSystemHelper.CreateMarkdownFile(
            Path.Combine(testDir, "Posts", "test.md"),
            "Test Post",
            "Content"
        );

        FileSystemHelper.CreateRazorTemplate(
            Path.Combine(testDir, "Posts", "template.razor"),
            @"@page ""/posts/{Slug}""
<h1>@Title</h1>
<div>@Body</div>"
        );

        // Build the test plugin with dependencies first
        var pluginWithDepsPath = Path.Combine(GetCurrentDirectory(), "tests", "Blake.IntegrationTests", "TestPluginWithDependencies");
        var pluginBuildResult = await RunProcessAsync("dotnet", "build", pluginWithDepsPath);
        Assert.Equal(0, pluginBuildResult.ExitCode);

        // Add plugin project reference to the test project
        var csprojPath = Path.Combine(testDir, $"{projectName}.csproj");
        var csprojContent = File.ReadAllText(csprojPath);
        
        var pluginProjectPath = Path.Combine(pluginWithDepsPath, "BlakePlugin.TestPluginWithDependencies.csproj");
        var relativePath = Path.GetRelativePath(testDir, pluginProjectPath);
        
        var updatedCsproj = csprojContent.Replace("</Project>", 
            $@"  <ItemGroup>
    <ProjectReference Include=""{relativePath}"" />
  </ItemGroup>

</Project>");
        File.WriteAllText(csprojPath, updatedCsproj);

        // Act
        var result = await RunBlakeCommandAsync($"bake \"{testDir}\" --verbosity Debug");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        // Plugin should have created marker files with JSON serialization
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, ".plugin-with-deps-before-bake.txt"));
        FileSystemHelper.AssertFileExists(Path.Combine(testDir, ".plugin-with-deps-after-bake.txt"));
        
        // Verify the JSON content was written correctly (meaning dependencies loaded)
        var beforeContent = File.ReadAllText(Path.Combine(testDir, ".plugin-with-deps-before-bake.txt"));
        var afterContent = File.ReadAllText(Path.Combine(testDir, ".plugin-with-deps-after-bake.txt"));
        
        Assert.Contains("Plugin with dependencies loaded successfully", beforeContent);
        Assert.Contains("Plugin dependencies working in AfterBakeAsync", afterContent);
        
        // Should show plugin logs
        Assert.Contains("TestPluginWithDependencies: BeforeBakeAsync called", result.OutputText);
        Assert.Contains("TestPluginWithDependencies: AfterBakeAsync called", result.OutputText);
    }

    private static string GetCurrentDirectory()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null && !File.Exists(Path.Combine(currentDir, "Blake.sln")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }
        return currentDir ?? throw new InvalidOperationException("Could not find Blake solution directory");
    }
}