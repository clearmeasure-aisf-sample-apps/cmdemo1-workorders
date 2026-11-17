using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>
/// Browser script for <c>js/theme.js</c>: read, set and sync the light/dark theme preference.
/// </summary>
public sealed class ThemeScript(IJSRuntime js) : IAsyncDisposable
{
    public const string ModulePath = "./_content/ClearMeasure.Bootcamp.UI.Shared/js/theme.js";

    private IJSObjectReference? _module;

    public async Task<string?> GetThemeAsync()
    {
        try
        {
            return await (await ModuleAsync()).InvokeAsync<string>("getTheme");
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone during theme init; keep the in-memory default.
            return null;
        }
        catch (JSException)
        {
            // JS error during theme read; keep the in-memory default.
            return null;
        }
    }

    public async Task SyncDomFromThemeAsync(string theme)
    {
        try
        {
            await (await ModuleAsync()).InvokeVoidAsync("syncDomFromTheme", theme);
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone during theme init; DOM sync is best-effort.
        }
        catch (JSException)
        {
            // JS error during DOM sync; best-effort.
        }
    }

    public async Task SetThemeAsync(bool enabled)
    {
        try
        {
            await (await ModuleAsync()).InvokeVoidAsync("setTheme", enabled);
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone while toggling theme; in-memory state still updates.
        }
        catch (JSException)
        {
            // JS error during theme toggle; best-effort.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
            return;

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone during teardown; module dispose is best-effort.
        }

        _module = null;
    }

    private async Task<IJSObjectReference> ModuleAsync()
    {
        if (_module is not null)
            return _module;

        #pragma warning disable BL0016 // -- reason: wrapper lets JS exceptions propagate to callers that catch them
        return _module = await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
        #pragma warning restore BL0016
    }
}
