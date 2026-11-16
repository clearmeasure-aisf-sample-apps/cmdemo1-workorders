using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class WorkOrderSearchBoardEmptyColumnTests
{
    [Test]
    public async Task ShouldHaveRoleStatusOnEmptyColumnMessage_WhenAllColumnsAreEmpty()
    {
        await using var ctx = new BunitContext();

        var board = ctx.Render<WorkOrderSearchBoard>(parameters => parameters
            .Add(p => p.Results, Array.Empty<WorkOrderSearchResultRow>()));

        var emptyElements = board.FindAll(".board-column-empty");
        emptyElements.ShouldNotBeEmpty();
        emptyElements.All(e => e.GetAttribute("role") == "status").ShouldBeTrue();
    }
}
