using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

/// <summary>
/// One browser ES module, imported on first use and released with the script class that owns it.
/// A circuit that is gone means the module is unavailable: <see cref="GetAsync"/> returns null and
/// <see cref="DisposeAsync(string?)"/> does nothing, so every <c>*Script</c> class treats browser work as
/// best-effort the same way. A JavaScript error (for example a missing module) still reaches the caller.
/// </summary>
public sealed class LazyJsModule(IJSRuntime js, string modulePath) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    /// <summary>The imported module, or null when the circuit disconnected before the import finished.</summary>
    public async Task<IJSObjectReference?> GetAsync()
    {
        if (_module is not null)
            return _module;

        try
        {
            _module = await js.InvokeAsync<IJSObjectReference>("import", modulePath);
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone before the import finished; the script is unavailable.
        }

        return _module;
    }

    /// <summary>Releases a module that was imported; a module never imported is not imported just to dispose it.</summary>
    public ValueTask DisposeAsync() => DisposeAsync(cleanupFunction: null);

    /// <summary>
    /// Calls <paramref name="cleanupFunction"/> on the module (when given and the module was imported), then releases it.
    /// </summary>
    public async ValueTask DisposeAsync(string? cleanupFunction)
    {
        var module = _module;
        if (module is null)
            return;

        _module = null;
        try
        {
            if (cleanupFunction is not null)
            {
                await module.InvokeVoidAsync(cleanupFunction);
            }

            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone during teardown; module dispose is best-effort.
        }
    }
}