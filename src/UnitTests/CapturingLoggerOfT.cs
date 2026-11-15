using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UnitTests;

/// <summary>
/// Typed facade over <see cref="CapturingLogger"/> for <c>ILogger&lt;T&gt;</c> constructor parameters.
/// </summary>
public sealed class CapturingLogger<T> : ILogger<T>
{
    public CapturingLogger()
        : this(new CapturingLogger())
    {
    }

    public CapturingLogger(CapturingLogger inner) => Inner = inner;

    public CapturingLogger Inner { get; }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
        Inner.BeginScope(state) ?? EmptyScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => Inner.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Inner.Log(logLevel, eventId, state, exception, formatter);

    private sealed class EmptyScope : IDisposable
    {
        public static readonly EmptyScope Instance = new();

        public void Dispose()
        {
        }
    }
}
