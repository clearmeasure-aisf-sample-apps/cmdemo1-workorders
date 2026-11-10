using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Components.Search;

/// <summary>
/// The search page's browser script (<c>js/searchExport.js</c>), imported on first use: CSV download, focus by id,
/// and the indeterminate state of the select-all checkbox.
/// </summary>
public sealed class SearchPageScript(IJSRuntime js)
{
    private IJSObjectReference? _module;

    public async Task DownloadCsvAsync(string fileName, string csv) =>
        await (await ModuleAsync()).InvokeVoidAsync("downloadCsv", fileName, csv);

    public async Task FocusByIdAsync(string elementId) =>
        await (await ModuleAsync()).InvokeVoidAsync("focusById", elementId);

    public async Task SetIndeterminateAsync(ElementReference checkbox, bool indeterminate) =>
        await (await ModuleAsync()).InvokeVoidAsync("setIndeterminate", checkbox, indeterminate);

    private async Task<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import",
            "./_content/ClearMeasure.Bootcamp.UI.Shared/js/searchExport.js");
}
