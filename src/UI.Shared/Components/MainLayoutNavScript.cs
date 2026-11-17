using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

/// <summary>
/// Browser script for <c>js/mainLayoutNav.js</c>: nav-rail viewport sync and the global-search Ctrl+K shortcut.
/// </summary>
public sealed class MainLayoutNavScript(IJSRuntime js) : IAsyncDisposable
{
    public const string ModulePath = "./_content/ClearMeasure.Bootcamp.UI.Shared/js/mainLayoutNav.js";

    private readonly LazyJsModule _module = new(js, ModulePath);
    private IJSObjectReference? _navToggleHelper;
    private IJSObjectReference? _globalSearchShortcut;

    public async Task InitNavToggleAsync(DotNetObjectReference<NavRail> dotNetRef, string mediaQuery,
        Func<bool> isDisposed)
    {
        try
        {
            var module = await _module.GetAsync();
            if (module is null || isDisposed())
                return;

            _navToggleHelper = await module.InvokeAsync<IJSObjectReference>("initNavToggle", dotNetRef, mediaQuery);
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone during shell init; nav toggle stays in its default CSS state.
        }
    }

    public async Task InitGlobalSearchShortcutAsync(string inputId)
    {
        try
        {
            var module = await _module.GetAsync();
            if (module is null)
                return;

            _globalSearchShortcut = await module.InvokeAsync<IJSObjectReference>("initGlobalSearchShortcut", inputId);
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone during shell init; Ctrl+K shortcut is best-effort.
        }
    }

    public async Task FocusAsync(ElementReference element)
    {
        try
        {
            await element.FocusAsync();
        }
        catch (InvalidOperationException)
        {
            // Element gone during overlay close / teardown; focus restore is best-effort.
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeHelperAsync(_navToggleHelper);
        _navToggleHelper = null;
        await DisposeHelperAsync(_globalSearchShortcut);
        _globalSearchShortcut = null;
        await _module.DisposeAsync();
    }

    private static async Task DisposeHelperAsync(IJSObjectReference? helper)
    {
        if (helper is null)
            return;

        try
        {
            await helper.InvokeVoidAsync("dispose");
            await helper.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone during teardown; helper dispose is best-effort.
        }
    }
}