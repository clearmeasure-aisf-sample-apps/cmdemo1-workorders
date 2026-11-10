using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class WorkOrderSearchBoardTests
{
    [Test]
    public async Task ShouldPutEachCardInItsStatusColumn_WhenRendered()
    {
        await using var ctx = new BunitContext();
        var rows = new[] { SearchResultRowMother.WithStatus(WorkOrderStatus.Complete, "401") };

        var board = ctx.Render<WorkOrderSearchBoard>(parameters => parameters.Add(p => p.Results, rows));

        board.Find($"[data-testid='{WorkOrderSearch.Elements.BoardColumnCount}Complete']").TextContent.ShouldBe("1");
        board.Find($"[data-testid='{WorkOrderSearch.Elements.BoardColumn}Complete'] [data-testid='{WorkOrderSearch.Elements.BoardCard}401']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldTellScreenReadersTheCardIsUnassigned_WhenItHasNoAssignee()
    {
        await using var ctx = new BunitContext();

        var board = ctx.Render<WorkOrderSearchBoard>(parameters => parameters.Add(p => p.Results, [SearchResultRowMother.Unassigned("402")]));

        board.Find(".board-card-assignee .visually-hidden").TextContent.ShouldBe("Unassigned");
    }
}
