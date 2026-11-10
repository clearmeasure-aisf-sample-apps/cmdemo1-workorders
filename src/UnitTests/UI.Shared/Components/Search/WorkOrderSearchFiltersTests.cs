using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class WorkOrderSearchFiltersTests
{
    private static readonly SelectListItem[] Users = [new("ada", "Ada Lovelace"), new("ned", "Ned Flanders")];

    [Test]
    public async Task ShouldDisableTheAssignee_WhenAssignedToMeIsOn()
    {
        await using var ctx = new BunitContext();

        var filters = ctx.Render<WorkOrderSearchFilters>(parameters => parameters
            .Add(p => p.Filters, new WorkOrderSearchModel.SearchFilters())
            .Add(p => p.AssigneeLocked, true));

        filters.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}").HasAttribute("disabled").ShouldBeTrue();
    }

    [Test]
    public async Task ShouldBindTheCreatorAndRaiseChanged_WhenACreatorIsChosen()
    {
        await using var ctx = new BunitContext();
        var values = new WorkOrderSearchModel.SearchFilters();
        var changes = 0;
        var filters = ctx.Render<WorkOrderSearchFilters>(parameters => parameters
            .Add(p => p.Filters, values)
            .Add(p => p.UserOptions, Users)
            .Add(p => p.OnChanged, () => changes++));

        filters.Find($"#{WorkOrderSearch.Elements.CreatorSelect}").Change("ned");

        values.Creator.ShouldBe("ned");
        changes.ShouldBe(1);
    }

    [Test]
    public async Task ShouldDisableClearAll_WhenNoFilterIsActive()
    {
        await using var ctx = new BunitContext();

        var filters = ctx.Render<WorkOrderSearchFilters>(parameters => parameters
            .Add(p => p.Filters, new WorkOrderSearchModel.SearchFilters())
            .Add(p => p.HasActiveFilters, false));

        filters.Find($"#{WorkOrderSearch.Elements.ClearFiltersButton}").HasAttribute("disabled").ShouldBeTrue();
    }
}
