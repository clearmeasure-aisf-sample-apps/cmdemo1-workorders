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

        dueDate.Find($"[data-testid='{WorkOrderSearch.Elements.DueDateCell}301']").TextContent.ShouldBe("–");
        dueDate.Markup.ShouldContain("No due date");
    }

    [Test]
    public async Task ShouldShowTheDateWithAnOverdueBadge_WhenTheRowIsOverdue()
    {
        await using var ctx = new BunitContext();
        var row = SearchResultRowMother.OverdueSince(new DateOnly(2026, 1, 5), "302");

        var dueDate = ctx.Render<WorkOrderDueDate>(parameters => parameters.Add(p => p.Row, row));

        dueDate.Find($"[data-testid='{WorkOrderSearch.Elements.DueDateCell}302']").TextContent.ShouldStartWith("Jan 5, 2026");
        dueDate.Find($"[data-testid='{WorkOrderSearch.Elements.UrgencyBadge}302']").ClassList.ShouldContain("overdue");
    }
}
