using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Components.Search;

/// <summary>
/// The search page's browser script (<c>js/searchExport.js</c>), imported on first use: CSV download, focus by id,
/// and the indeterminate state of the select-all checkbox. Each call is best-effort: a circuit that is gone does nothing.
/// </summary>
public sealed class SearchPageScript(IJSRuntime js) : IAsyncDisposable
{
    public const string ModulePath = "./_content/ClearMeasure.Bootcamp.UI.Shared/js/searchExport.js";

    private readonly LazyJsModule _module = new(js, ModulePath);

    public Task DownloadCsvAsync(string fileName, string csv) => InvokeBestEffortAsync("downloadCsv", fileName, csv);

    public Task FocusByIdAsync(string elementId) => InvokeBestEffortAsync("focusById", elementId);

    public Task SetIndeterminateAsync(ElementReference checkbox, bool indeterminate) =>
        InvokeBestEffortAsync("setIndeterminate", checkbox, indeterminate);

    public ValueTask DisposeAsync() => _module.DisposeAsync();

    private async Task InvokeBestEffortAsync(string function, params object?[] args)
    {
        try
        {
            var module = await _module.GetAsync();
            if (module is null)
                return;

            await module.InvokeVoidAsync(function, args);
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone (navigation, re-render or teardown); there is no browser left to update.
        }
    }
}