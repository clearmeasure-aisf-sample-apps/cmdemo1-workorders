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

        _module = null;
    }

    private async Task<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
}
