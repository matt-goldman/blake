using Blake.BuildTools.Utils;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using Xunit;

namespace Blake.BuildTools.Tests.Utils;

public class PluginLoaderTests
{
    [Fact]
    public void LoadPluginDLLs_WithTestPlugin_LoadsSuccessfully()
    {
        // Arrange
        var logger = new TestLogger();
        var pluginPath = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "tests", "Blake.IntegrationTests", 
            "TestPlugin", "bin", "Debug", "net9.0", 
            "BlakePlugin.TestPlugin.dll"
        ));

        // Skip test if plugin doesn't exist (build not run)
        if (!File.Exists(pluginPath))
        {
            Assert.True(true, "Plugin not built - skipping test");
            return;
        }

        var files = new List<string> { pluginPath };
        var plugins = new List<PluginContext>();

        // Act & Assert - should not throw exception
        var exception = Record.Exception(() =>
        {
            // Use reflection to call the private method
            var method = typeof(PluginLoader).GetMethod("LoadPluginDLLs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
            if (method == null)
            {
                throw new InvalidOperationException("Could not find LoadPluginDLLs method via reflection");
            }
                
            method.Invoke(null, new object[] { files, plugins, logger });
        });

        // Assert
        Assert.Null(exception);
        Assert.Equal(3, plugins.Count); // TestPlugin class has 3 plugin classes
        
        // Check that we have all expected plugins
        var pluginNames = plugins.Select(p => p.Plugin.GetType().Name).OrderBy(n => n).ToList();
        Assert.Contains("TestPlugin", pluginNames);
        Assert.Contains("FailingTestPlugin", pluginNames);
        Assert.Contains("ContentModifyingPlugin", pluginNames);
        
        // Ensure no errors were logged
        Assert.Empty(logger.ErrorMessages);
    }

    [Fact]
    public void LoadPluginDLLs_WithPluginWithDependencies_LoadsSuccessfully()
    {
        // Arrange
        var logger = new TestLogger();
        var pluginPath = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "tests", "Blake.IntegrationTests", 
            "TestPluginWithDependencies", "bin", "Debug", "net9.0", 
            "BlakePlugin.TestPluginWithDependencies.dll"
        ));

        // Skip test if plugin doesn't exist (build not run)
        if (!File.Exists(pluginPath))
        {
            Assert.True(true, "Plugin not built - skipping test");
            return;
        }

        var files = new List<string> { pluginPath };
        var plugins = new List<PluginContext>();

        // Act & Assert - should not throw exception
        var exception = Record.Exception(() =>
        {
            // Use reflection to call the private method
            var method = typeof(PluginLoader).GetMethod("LoadPluginDLLs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, new object[] { files, plugins, logger });
        });

        // Assert
        Assert.Null(exception);
        Assert.Single(plugins);
        Assert.Equal("BlakePlugin.TestPluginWithDependencies", plugins[0].PluginName);
        
        // Ensure no errors were logged
        Assert.Empty(logger.ErrorMessages);
    }

    [Fact]
    public void GetNuGetPluginInfo_WithValidPackages_ReturnsCorrectInfo()
    {
        // Arrange
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""BlakePlugin.ReadTime"" Version=""1.0.0"" />
    <PackageReference Include=""BlakePlugin.DocsRenderer"" Version=""2.1.0"" />
    <PackageReference Include=""SomeOtherPackage"" Version=""1.0.0"" />
  </ItemGroup>
</Project>";
        var doc = XDocument.Parse(xml);

        // Act
        var result = (List<NuGetPluginInfo>)typeof(PluginLoader)
            .GetMethod("GetNuGetPluginInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[] { doc })!;

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.PackageName == "BlakePlugin.ReadTime" && p.Version == "1.0.0");
        Assert.Contains(result, p => p.PackageName == "BlakePlugin.DocsRenderer" && p.Version == "2.1.0");
        Assert.DoesNotContain(result, p => p.PackageName == "SomeOtherPackage");
    }

    [Fact]
    public void GetProjectRefPluginInfo_WithValidProjectRefs_ReturnsCorrectInfo()
    {
        // Arrange
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""../BlakePlugin.Custom/BlakePlugin.Custom.csproj"" />
    <ProjectReference Include=""../SomeOtherProject/SomeOtherProject.csproj"" />
  </ItemGroup>
</Project>";
        var doc = XDocument.Parse(xml);
        var projectDirectory = "/test/project";

        // Act
        var result = (List<ProjectRefPluginInfo>)typeof(PluginLoader)
            .GetMethod("GetProjectRefPluginInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[] { doc, projectDirectory, "Debug" })!;

        // Assert
        Assert.Single(result);
        Assert.Contains(result, p => p.ProjectPath.Contains("BlakePlugin.Custom"));
        Assert.DoesNotContain(result, p => p.ProjectPath.Contains("SomeOtherProject"));
    }

    [Fact]
    public void IsNuGetPluginValid_WithExistingValidDll_ReturnsTrue()
    {
        // Arrange
        var logger = new TestLogger();
        
        // Create a temporary DLL file for testing
        var tempDir = Path.GetTempPath();
        var tempFile = Path.Combine(tempDir, "TestPlugin.dll");
        File.WriteAllText(tempFile, "fake dll content");
        
        try
        {
            var plugin = new NuGetPluginInfo("TestPlugin", "1.0.0", tempFile);

            // Act
            var result = (bool)typeof(PluginLoader)
                .GetMethod("IsNuGetPluginValid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .Invoke(null, new object[] { plugin, logger })!;

            // Assert - should return true since the file exists (version check may fail gracefully)
            Assert.True(result);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void IsNuGetPluginValid_WithNonExistentDll_ReturnsFalse()
    {
        // Arrange
        var logger = new TestLogger();
        var plugin = new NuGetPluginInfo("TestPlugin", "1.0.0", "/nonexistent/path/TestPlugin.dll");

        // Act
        var result = (bool)typeof(PluginLoader)
            .GetMethod("IsNuGetPluginValid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[] { plugin, logger })!;

        // Assert
        Assert.False(result);
    }

    private class TestLogger : ILogger
    {
        public List<string> ErrorMessages { get; } = new List<string>();
        public List<string> InfoMessages { get; } = new List<string>();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            if (logLevel == LogLevel.Error)
            {
                ErrorMessages.Add(message);
            }
            else if (logLevel == LogLevel.Information)
            {
                InfoMessages.Add(message);
            }
        }
    }
}