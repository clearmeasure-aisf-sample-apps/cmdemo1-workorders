using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Components.Search;

/// <summary>
/// The search page's browser script (<c>js/searchExport.js</c>), imported on first use: CSV download, focus by id,
/// and the indeterminate state of the select-all checkbox.
/// </summary>
public sealed class SearchPageScript(IJSRuntime js) : IAsyncDisposable
{
    public const string ModulePath = "./_content/ClearMeasure.Bootcamp.UI.Shared/js/searchExport.js";

    private IJSObjectReference? _module;

    public async Task DownloadCsvAsync(string fileName, string csv) =>
        await (await ModuleAsync()).InvokeVoidAsync("downloadCsv", fileName, csv);

    public async Task FocusByIdAsync(string elementId) =>
        await (await ModuleAsync()).InvokeVoidAsync("focusById", elementId);

    public async Task SetIndeterminateAsync(ElementReference checkbox, bool indeterminate)
    {
        try
        {
            await (await ModuleAsync()).InvokeVoidAsync("setIndeterminate", checkbox, indeterminate);
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone during table re-render; indeterminate state is best-effort.
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
