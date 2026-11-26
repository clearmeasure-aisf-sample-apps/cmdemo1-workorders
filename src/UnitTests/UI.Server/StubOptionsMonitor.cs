using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>
/// An <see cref="IOptionsMonitor{TOptions}"/> that always answers the given value; change notifications never fire.
/// </summary>
internal sealed class StubOptionsMonitor<T>(T value) : IOptionsMonitor<T> where T : class
{
    public T CurrentValue => value;

    public T Get(string? name) => value;

    public IDisposable OnChange(Action<T, string?> listener) => new NoOpDisposable();

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
