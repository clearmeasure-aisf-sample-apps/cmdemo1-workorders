using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class WorkOrderSearchTableTests
{
    private BunitContext _ctx = null!;

    [SetUp]
    public void CreateContextWithLooseScripts()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TearDown]
    public void DisposeContext() => _ctx.Dispose();

    [Test]
    public void ShouldExposeWorkOrdersAsTheAccessibleName_OnTheResultsTable()
    {
        var table = RenderTable(SearchResultRowMother.Numbered("500"), new WorkOrderRowSelection());

        table.Find("table.grid-data").GetAttribute("aria-label").ShouldBe("Work orders");
    }

    [Test]
    public void ShouldMarkTheSortedColumnDescending_WhenSortedDescending()
    {
        var table = RenderTable(SearchResultRowMother.Numbered("501"), new WorkOrderRowSelection(), SortColumns.Title);

        var header = table.Find($"#{WorkOrderSearch.Elements.SortByTitleButton}");

        header.TextContent.Trim().ShouldBe("Title ▼");
        header.ParentElement!.GetAttribute("aria-sort").ShouldBe("descending");
    }

    [Test]
    public void ShouldSelectTheRowAndReportIt_WhenItsCheckboxIsTicked()
    {
        var selection = new WorkOrderRowSelection();
        var reported = 0;
        var table = RenderTable(SearchResultRowMother.Numbered("502"), selection, onSelectionChanged: () => reported++);

        table.Find($"[data-testid='{WorkOrderSearch.Elements.RowCheckbox}502']").Change(true);

        selection.IsSelected("502").ShouldBeTrue();
        reported.ShouldBe(1);
    }

    [Test]
    public void ShouldSelectEveryRowOfThePage_WhenSelectAllIsTicked()
    {
        var selection = new WorkOrderRowSelection();
        var table = RenderTable(SearchResultRowMother.Numbered("503", "504"), selection);

        table.Find($"[data-testid='{WorkOrderSearch.Elements.SelectAllCheckbox}']").Change(true);

        selection.Numbers.ShouldBe(["503", "504"], ignoreOrder: true);
    }

    [Test]
    public void ShouldCloseTheRowMenu_WhenThePageAsksToClose()
    {
        var table = RenderTable(SearchResultRowMother.Numbered("505"), new WorkOrderRowSelection());
        table.Find($"[data-testid='{WorkOrderSearch.Elements.RowMenu}505']").Click();

        table.InvokeAsync(table.Instance.CloseRowMenu);
        table.Render();

        table.FindAll($"[data-testid='{WorkOrderSearch.Elements.RowMenuOpen}505']").ShouldBeEmpty();
    }

    [Test]
    public void ShouldShowADashAndSayThereIsNoRoom_WhenTheRowHasNone()
    {
        var table = RenderTable(SearchResultRowMother.Numbered("506"), new WorkOrderRowSelection());

        var cell = table.Find($"[data-testid='{WorkOrderSearch.Elements.RoomCell}506']");
        var dash = cell.QuerySelector(".muted-cell");
        dash.ShouldNotBeNull();
        dash.GetAttribute("aria-hidden").ShouldBe("true");
        dash.HasAttribute("aria-label").ShouldBeFalse();
        dash.TextContent.ShouldBe("–");
        cell.QuerySelector(".visually-hidden")!.TextContent.ShouldBe("No room");
    }

    private IRenderedComponent<WorkOrderSearchTable> RenderTable(WorkOrderSearchResultRow[] rows, WorkOrderRowSelection selection,
        string? sortColumn = null, Action? onSelectionChanged = null) =>
        _ctx.Render<WorkOrderSearchTable>(parameters => parameters
            .Add(p => p.Rows, rows)
            .Add(p => p.Selection, selection)
            .Add(p => p.SortColumn, sortColumn)
            .Add(p => p.Descending, true)
            .Add(p => p.OnSelectionChanged, onSelectionChanged ?? (() => { })));
}
