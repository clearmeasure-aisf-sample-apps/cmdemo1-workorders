using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class WorkOrderSearchFiltersRoomAutocompleteTests
{
    [Test]
    public async Task ShouldTurnOffAutofillOnTheRoomFilter()
    {
        await using var ctx = new BunitContext();

        var filters = ctx.Render<WorkOrderSearchFilters>(parameters => parameters
            .Add(p => p.Filters, new WorkOrderSearchModel.SearchFilters()));

        filters.Find($"#{WorkOrderSearch.Elements.RoomInput}").GetAttribute("autocomplete").ShouldBe("off");
    }
}
