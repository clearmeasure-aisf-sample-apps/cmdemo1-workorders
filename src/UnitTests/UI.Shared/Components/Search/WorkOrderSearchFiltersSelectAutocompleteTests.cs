using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using NUnit.Framework;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class WorkOrderSearchFiltersSelectAutocompleteTests
{
    [TestCase(WorkOrderSearch.Elements.CreatorSelect)]
    [TestCase(WorkOrderSearch.Elements.AssigneeSelect)]
    [TestCase(WorkOrderSearch.Elements.StatusSelect)]
    [TestCase(WorkOrderSearch.Elements.PrioritySelect)]
    [TestCase(WorkOrderSearch.Elements.BuildingSelect)]
    public async Task ShouldTurnOffAutofillOnSelectFilters(WorkOrderSearch.Elements selectId)
    {
        await using var ctx = new BunitContext();

        var filters = ctx.Render<WorkOrderSearchFilters>(parameters => parameters
            .Add(p => p.Filters, new WorkOrderSearchModel.SearchFilters()));

        filters.Find($"#{selectId}").GetAttribute("autocomplete").ShouldBe("off");
    }
}
