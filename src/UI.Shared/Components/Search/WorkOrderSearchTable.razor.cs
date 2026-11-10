using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Components.Search;

/// <summary>
/// One page of search results as a table: sortable headers, a selection checkbox per row and for the page,
/// and a row menu.
/// </summary>
public partial class WorkOrderSearchTable
{
    private ElementReference _selectAllCheckbox;
    private string? _openRowMenu;
    private SearchPageScript? _script;

    [Inject] private IJSRuntime Js { get; set; } = null!;

    /// <summary>The rows of the current page.</summary>
    [Parameter, EditorRequired] public WorkOrderSearchResultRow[] Rows { get; set; } = [];

    /// <summary>The selected rows across pages; the table toggles it and reports each change.</summary>
    [Parameter, EditorRequired] public WorkOrderRowSelection Selection { get; set; } = null!;

    [Parameter] public string? SortColumn { get; set; }
    [Parameter] public bool Descending { get; set; }
    [Parameter] public bool ShowPriorityBadges { get; set; }
    [Parameter] public EventCallback<string> OnSortBy { get; set; }
    [Parameter] public EventCallback OnSelectionChanged { get; set; }

    private string[] PageNumbers => Rows.Select(r => r.Number).ToArray();

    /// <summary>Closes an open row menu; the page calls it whenever the search, sort, page or view changes.</summary>
    public void CloseRowMenu() => _openRowMenu = null;

    /// <summary>Header button text: the label, plus " ▲" or " ▼" on the sorted column.</summary>
    private string SortLabel(string column, string label) =>
        SortColumn != column ? label : $"{label} {(Descending ? "▼" : "▲")}";

    private string AriaSort(string column) =>
        SortColumn != column ? "none" : Descending ? "descending" : "ascending";

    private string RowClass(WorkOrderSearchResultRow row)
    {
        var classes = new List<string>();
        AddClassIf(classes, row.WorkOrder.IsOverdue(), "overdue-row");
        AddClassIf(classes, Selection.IsSelected(row.Number), "row-selected");
        return string.Join(" ", classes);
    }

    private static void AddClassIf(List<string> classes, bool condition, string cssClass)
    {
        if (condition)
        {
            classes.Add(cssClass);
        }
    }

    private Task HandleRowToggled(string number)
    {
        Selection.Toggle(number);
        return OnSelectionChanged.InvokeAsync();
    }

    private Task HandleSelectAllChanged()
    {
        Selection.TogglePage(PageNumbers);
        return OnSelectionChanged.InvokeAsync();
    }

    private void ToggleRowMenu(string number) => _openRowMenu = _openRowMenu == number ? null : number;

    private async Task HandleRowMenuKeyDown(KeyboardEventArgs e)
    {
        if (e.Key != "Escape" || _openRowMenu == null)
        {
            return;
        }

        var trigger = $"row-menu-{_openRowMenu}";
        _openRowMenu = null;
        await Script.FocusByIdAsync(trigger);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Rows.Length == 0)
        {
            return;
        }

        try
        {
            await Script.SetIndeterminateAsync(_selectAllCheckbox, Selection.IsIndeterminate(PageNumbers));
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private SearchPageScript Script => _script ??= new SearchPageScript(Js);
}
