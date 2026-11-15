using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

/// <summary>
/// Browser script for <c>js/clipboard.js</c>: copy text to the system clipboard.
/// </summary>
public sealed class ClipboardScript(IJSRuntime js) : IAsyncDisposable
{
    public const string ModulePath = "./_content/ClearMeasure.Bootcamp.UI.Shared/js/clipboard.js";

    private IJSObjectReference? _module;

    public async Task<bool> CopyToClipboardAsync(string text)
    {
        try
        {
            await (await ModuleAsync()).InvokeAsync<object>("copyToClipboard", text);
            return true;
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone or clipboard unavailable; copy is best-effort.
            return false;
        }
        catch (JSException)
        {
            // Clipboard permission denied or other JS error; copy is best-effort.
            return false;
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

    private async Task<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
}