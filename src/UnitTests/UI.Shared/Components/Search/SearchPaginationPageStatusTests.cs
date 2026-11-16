using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class SearchPaginationPageStatusTests
{
    private static readonly WorkOrderSearchResultRow[] ThreePagesOfRows =
        Enumerable.Range(1, 60).Select(n => SearchResultRowMother.Unassigned(n.ToString())).ToArray();

    [Test]
    public void PageText_ShouldReadPageTwoOfThree_WhenOnSecondPage()
    {
        var page = WorkOrderSearchPager.GetPage(ThreePagesOfRows, 2);

        page.PageText.ShouldBe("Page 2 of 3");
    }

    [Test]
    public async Task ShouldRenderPageStatus_WithRoleAndText()
    {
        await using var ctx = new BunitContext();

        var pagination = ctx.Render<SearchPagination>(parameters => parameters
            .Add(p => p.Page, WorkOrderSearchPager.GetPage(ThreePagesOfRows, 2)));

        var statusElement = pagination.Find($"[data-testid='{WorkOrderSearch.Elements.PageStatus}']");

        statusElement.GetAttribute("role").ShouldBe("status");
        statusElement.TextContent.ShouldBe("Page 2 of 3");
    }
}
