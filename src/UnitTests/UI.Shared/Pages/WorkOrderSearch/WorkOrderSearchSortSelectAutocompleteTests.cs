using Bunit;
using Shouldly;
using WorkOrderSearchPage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderSearch;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

public class WorkOrderSearchSortSelectAutocompleteTests
{
    [Test]
    public async Task SortSelect_HasAutocompleteOff_ToPreventBrowserAutofill()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        var sortSelect = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.SortSelect);

        sortSelect.GetAttribute("autocomplete").ShouldBe("off");
    }
}
