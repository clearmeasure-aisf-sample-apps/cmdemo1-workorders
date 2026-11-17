using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

/// <summary>
/// Browser script for <c>js/notificationsPanel.js</c>: Escape closes the panel and returns focus to the bell.
/// </summary>
public sealed class NotificationsPanelScript(IJSRuntime js) : IAsyncDisposable
{
    public const string ModulePath = "./_content/ClearMeasure.Bootcamp.UI.Shared/js/notificationsPanel.js";

    private readonly LazyJsModule _module = new(js, ModulePath);

    public async Task SetupEscapeHandlerAsync(DotNetObjectReference<NotificationsPanel> dotNetRef)
    {
        try
        {
            var module = await _module.GetAsync();
            if (module is null)
                return;

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

    public ValueTask DisposeAsync() => _module.DisposeAsync(cleanupFunction: "cleanupEscapeHandler");
}