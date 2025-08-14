using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Blake.IntegrationTests.Infrastructure;

public sealed class InMemoryLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentBag<CapturedLog> _logs = [];
    public IReadOnlyCollection<CapturedLog> Logs => _logs;

    public ILogger CreateLogger(string categoryName) => new InMemoryLogger(_logs, categoryName);
    public void Dispose() { }

    private sealed class InMemoryLogger : ILogger
    {
        private readonly ConcurrentBag<CapturedLog> _sink;
        private readonly string _category;

        public InMemoryLogger(ConcurrentBag<CapturedLog> sink, string category) =>
            (_sink, _category) = (sink, category);

#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
#pragma warning restore CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            _sink.Add(new CapturedLog(
                logLevel, _category, eventId, formatter(state, exception), exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}

public record CapturedLog(
    LogLevel Level,
    string Category,
    EventId EventId,
    string Message,
    Exception? Exception);

