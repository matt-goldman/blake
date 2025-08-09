using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Blake.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration test fixtures that provides common setup and cleanup functionality.
/// </summary>
public abstract class TestFixtureBase : IDisposable
{
    protected readonly ILogger Logger;
    private readonly List<string> _tempDirectories = [];

    protected TestFixtureBase()
    {
        Logger = CreateLogger();
    }

    /// <summary>
    /// Creates a temporary directory for testing purposes.
    /// The directory will be automatically cleaned up when the test fixture is disposed.
    /// </summary>
    protected string CreateTempDirectory(string? prefix = null)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), prefix ?? "blake-test", Guid.NewGuid().ToString()[..8]);
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);
        return tempDir;
    }

    /// <summary>
    /// Runs a CLI command using the blake executable.
    /// </summary>
    protected async Task<ProcessResult> RunBlakeCommandAsync(string command, string workingDirectory = "", CancellationToken cancellationToken = default)
    {
        return await RunProcessAsync("dotnet", $"run --project \"{GetCliProjectPath()}\" -- {command}", workingDirectory, cancellationToken);
    }

    /// <summary>
    /// Runs an arbitrary process and captures the result.
    /// </summary>
    protected async Task<ProcessResult> RunProcessAsync(string fileName, string arguments, string workingDirectory = "", CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? Directory.GetCurrentDirectory() : workingDirectory
        };

        var outputBuilder = new List<string>();
        var errorBuilder = new List<string>();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                outputBuilder.Add(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                errorBuilder.Add(e.Data);
        };

        var startTime = DateTime.UtcNow;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult(
            ExitCode: process.ExitCode,
            Output: outputBuilder,
            Error: errorBuilder,
            Duration: DateTime.UtcNow - startTime
        );
    }

    /// <summary>
    /// Gets the path to the Blake CLI project for testing.
    /// </summary>
    private static string GetCliProjectPath()
    {
        // Navigate up from test directory to find the CLI project
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null && !File.Exists(Path.Combine(currentDir, "Blake.sln")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        if (currentDir == null)
        {
            throw new InvalidOperationException("Could not find Blake solution directory");
        }

        return Path.Combine(currentDir, "src", "Blake.CLI", "Blake.CLI.csproj");
    }

    private static ILogger CreateLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        return loggerFactory.CreateLogger("IntegrationTest");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var tempDir in _tempDirectories.Where(Directory.Exists))
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to clean up temp directory: {TempDir}", tempDir);
                }
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Result of running a process during integration testing.
/// </summary>
public record ProcessResult(
    int ExitCode,
    IReadOnlyList<string> Output,
    IReadOnlyList<string> Error,
    TimeSpan Duration
)
{
    public string OutputText => string.Join(Environment.NewLine, Output);
    public string ErrorText => string.Join(Environment.NewLine, Error);
    public bool IsSuccess => ExitCode == 0;
}