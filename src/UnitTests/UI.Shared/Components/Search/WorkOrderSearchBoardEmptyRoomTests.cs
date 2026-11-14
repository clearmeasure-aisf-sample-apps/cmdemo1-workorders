using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class WorkOrderSearchBoardEmptyRoomTests
{
    [Test]
    public async Task ShouldShowADashAndSayThereIsNoRoom_WhenTheCardHasNone()
    {
        await using var ctx = new BunitContext();

        var board = ctx.Render<WorkOrderSearchBoard>(parameters => parameters
            .Add(p => p.Results, [SearchResultRowMother.Unassigned("403")]));

        var room = board.Find(".board-card-room");
        room.HasAttribute("aria-label").ShouldBeFalse();
        var dash = room.QuerySelector(".muted-cell");
        dash.ShouldNotBeNull();
        dash.GetAttribute("aria-hidden").ShouldBe("true");
        dash.HasAttribute("aria-label").ShouldBeFalse();
        dash.TextContent.ShouldBe("–");
        room.QuerySelector(".visually-hidden")!.TextContent.ShouldBe("No room");
    }
}
