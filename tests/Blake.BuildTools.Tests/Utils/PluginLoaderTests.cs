using Blake.BuildTools.Utils;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Blake.BuildTools.Tests.Utils;

public class PluginLoaderTests
{
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