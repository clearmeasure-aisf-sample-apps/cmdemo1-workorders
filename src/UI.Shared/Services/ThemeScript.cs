using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>
/// Browser script for <c>js/theme.js</c>: read, set and sync the light/dark theme preference.
/// </summary>
public sealed class ThemeScript(IJSRuntime js) : IAsyncDisposable
{
    public const string ModulePath = "./_content/ClearMeasure.Bootcamp.UI.Shared/js/theme.js";

    private readonly LazyJsModule _module = new(js, ModulePath);

    public async Task<string?> GetThemeAsync()
    {
        try
        {
            var module = await _module.GetAsync();
            return module is null ? null : await module.InvokeAsync<string>("getTheme");
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone during theme init; keep the in-memory default.
            return null;
        }
    }

    public Task SyncDomFromThemeAsync(string theme) => InvokeBestEffortAsync("syncDomFromTheme", theme);

    public Task SetThemeAsync(bool enabled) => InvokeBestEffortAsync("setTheme", enabled);

    public ValueTask DisposeAsync() => _module.DisposeAsync();

    private async Task InvokeBestEffortAsync(string function, object argument)
    {
        try
        {
            var module = await _module.GetAsync();
            if (module is null)
                return;

            await module.InvokeVoidAsync(function, argument);
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone during theme init or toggle; the in-memory state still updates.
        }
    }
}