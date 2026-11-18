using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

/// <summary>
/// Browser script for <c>js/notificationsPanel.js</c>: Escape closes the panel and returns focus to the bell.
/// </summary>
public sealed class NotificationsPanelScript(IJSRuntime js) : IAsyncDisposable
{
    public const string ModulePath = "./_content/ClearMeasure.Bootcamp.UI.Shared/js/notificationsPanel.js";

    private IJSObjectReference? _module;

    public async Task SetupEscapeHandlerAsync(DotNetObjectReference<NotificationsPanel> dotNetRef)
    {
        try
        {
            var module = await ModuleAsync();
            await module.InvokeVoidAsync("setupEscapeHandler", dotNetRef);
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone while opening the panel; Escape handling is best-effort.
        }
        catch (JSException)
        {
            // JS error while setting up escape handler; best-effort.
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
            // Bell button gone during close / teardown; focus restore is best-effort.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
            return;

        try
        {
            await _module.InvokeVoidAsync("cleanupEscapeHandler");
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone during teardown; escape cleanup is best-effort.
        }
        catch (JSException)
        {
            // JS error during cleanup; best-effort.
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
