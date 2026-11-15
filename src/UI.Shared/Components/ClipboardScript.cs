using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

/// <summary>
/// Browser script for <c>js/clipboard.js</c>: copy text to the system clipboard.
/// </summary>
public sealed class ClipboardScript(IJSRuntime js) : IAsyncDisposable
{
    public const string ModulePath = "./_content/ClearMeasure.Bootcamp.UI.Shared/js/clipboard.js";

    private readonly LazyJsModule _module = new(js, ModulePath);

    public async Task<bool> CopyToClipboardAsync(string text)
    {
        try
        {
            var module = await _module.GetAsync();
            if (module is null)
                return false;

            await module.InvokeVoidAsync("copyToClipboard", text);
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

    public ValueTask DisposeAsync() => _module.DisposeAsync();
}