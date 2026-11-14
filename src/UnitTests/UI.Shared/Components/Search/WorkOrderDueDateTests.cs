using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class WorkOrderDueDateTests
{
    [Test]
    public async Task ShouldShowADashAndSayThereIsNoDueDate_WhenTheRowHasNone()
    {
        await using var ctx = new BunitContext();

        var dueDate = ctx.Render<WorkOrderDueDate>(parameters => parameters.Add(p => p.Row, SearchResultRowMother.Unassigned("301")));

        var cell = dueDate.Find($"[data-testid='{WorkOrderSearch.Elements.DueDateCell}301']");
        cell.TextContent.ShouldBe("–");
        cell.QuerySelector("[aria-hidden='true']")!.TextContent.ShouldBe("–");
        dueDate.Markup.ShouldContain("No due date");
    }

    [Test]
    public async Task ShouldShowTheDateWithAnOverdueBadge_WhenTheRowIsOverdue()
    {
        await using var ctx = new BunitContext();
        var row = SearchResultRowMother.OverdueSince(new DateOnly(2026, 1, 5), "302");

        var dueDate = ctx.Render<WorkOrderDueDate>(parameters => parameters.Add(p => p.Row, row));

        var cell = dueDate.Find($"[data-testid='{WorkOrderSearch.Elements.DueDateCell}302']");
        cell.TextContent.ShouldStartWith("Jan 5, 2026");
        cell.QuerySelector("[aria-hidden='true']").ShouldBeNull();
        dueDate.Find($"[data-testid='{WorkOrderSearch.Elements.UrgencyBadge}302']").ClassList.ShouldContain("overdue");
    }
}
