using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class SearchPaginationTests
{
    private static readonly WorkOrderSearchResultRow[] ThreePagesOfRows =
        Enumerable.Range(1, 60).Select(n => SearchResultRowMother.Unassigned(n.ToString())).ToArray();

    [Test]
    public async Task ShouldDisablePrevious_WhenOnTheFirstPage()
    {
        await using var ctx = new BunitContext();

        var pagination = ctx.Render<SearchPagination>(parameters => parameters
            .Add(p => p.Page, WorkOrderSearchPager.GetPage(ThreePagesOfRows, 1)));

        pagination.Find($"[data-testid='{WorkOrderSearch.Elements.PagePrevButton}']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Test]
    public async Task ShouldRaiseTheChosenPage_WhenAPageButtonIsClicked()
    {
        await using var ctx = new BunitContext();
        var chosen = 0;
        var pagination = ctx.Render<SearchPagination>(parameters => parameters
            .Add(p => p.Page, WorkOrderSearchPager.GetPage(ThreePagesOfRows, 1))
            .Add(p => p.OnPageSelected, page => chosen = page));

        await pagination.Find($"[data-testid='{WorkOrderSearch.Elements.PageButton}2']").ClickAsync(new());

        chosen.ShouldBe(2);
    }

    [Test]
    public async Task ShouldExposeDescriptiveAccessibleNames_OnPrevAndNextButtons()
    {
        await using var ctx = new BunitContext();

        var pagination = ctx.Render<SearchPagination>(parameters => parameters
            .Add(p => p.Page, WorkOrderSearchPager.GetPage(ThreePagesOfRows, 2)));

        pagination.Find($"[data-testid='{WorkOrderSearch.Elements.PagePrevButton}']")
            .GetAttribute("aria-label").ShouldBe("Prev page");
        pagination.Find($"[data-testid='{WorkOrderSearch.Elements.PageNextButton}']")
            .GetAttribute("aria-label").ShouldBe("Next page");
    }
}
