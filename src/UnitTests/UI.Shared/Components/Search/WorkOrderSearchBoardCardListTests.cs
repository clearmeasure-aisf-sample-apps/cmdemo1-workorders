using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class WorkOrderSearchBoardCardListTests
{
    [Test]
    public async Task ShouldNameTheCardListByItsColumnHeading()
    {
        await using var ctx = new BunitContext();

        var board = ctx.Render<WorkOrderSearchBoard>(parameters => parameters
            .Add(p => p.Results, [SearchResultRowMother.Unassigned("403")]));

        var cardList = board.Find("ul.board-cards");
        var labelledBy = cardList.GetAttribute("aria-labelledby");
        labelledBy.ShouldNotBeNullOrWhiteSpace();

        var heading = cardList.ParentElement!.QuerySelector("h3");
        heading.ShouldNotBeNull();
        heading.GetAttribute("id").ShouldBe(labelledBy);
    }
}
