using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

/// <summary>
/// Browser script for <c>js/mainLayoutNav.js</c>: nav-rail viewport sync and the global-search Ctrl+K shortcut.
/// </summary>
public sealed class MainLayoutNavScript(IJSRuntime js) : IAsyncDisposable
{
    public const string ModulePath = "./_content/ClearMeasure.Bootcamp.UI.Shared/js/mainLayoutNav.js";

    private IJSObjectReference? _module;
    private IJSObjectReference? _navToggleHelper;
    private IJSObjectReference? _globalSearchShortcut;

    public async Task InitNavToggleAsync(DotNetObjectReference<NavRail> dotNetRef, string mediaQuery,
        Func<bool> isDisposed)
    {
        try
        {
            var module = await ModuleAsync();
            if (isDisposed())
                return;

            _navToggleHelper = await module.InvokeAsync<IJSObjectReference>("initNavToggle", dotNetRef, mediaQuery);
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone during shell init; nav toggle stays in its default CSS state.
        }
        catch (JSException)
        {
            // JS error during nav toggle init; best-effort.
        }
    }

    public async Task InitGlobalSearchShortcutAsync(string inputId)
    {
        try
        {
            var module = await ModuleAsync();
            _globalSearchShortcut = await module.InvokeAsync<IJSObjectReference>("initGlobalSearchShortcut", inputId);
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone during shell init; Ctrl+K shortcut is best-effort.
        }
        catch (JSException)
        {
            // JS error during global search init; best-effort.
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

        if (_module is not null)
        {
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
    }

    private async Task DisposeHelperAsync(IJSObjectReference? helper)
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
        catch (JSException)
        {
            // JS error during helper dispose; best-effort.
        }
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
