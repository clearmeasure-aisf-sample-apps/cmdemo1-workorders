using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UnitTests;

/// <summary>
/// Test logger that records every call for assertions about level and message.
/// </summary>
public sealed class CapturingLogger : ILogger
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public int ErrorCount => Entries.Count(entry => entry.Level == LogLevel.Error);

    public IReadOnlyList<string> MessagesAt(LogLevel level) =>
        Entries.Where(entry => entry.Level == level).Select(entry => entry.Message).ToList();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception), exception));
    }
}
