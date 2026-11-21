using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class WorkOrderBulkActionBarAutocompleteTests
{
    [Test]
    public void BulkAssignSelect_HasAutocompleteOff()
    {
        using var ctx = new BunitContext();
        var component = ctx.Render<WorkOrderBulkActionBar>(p => p
            .Add(c => c.SelectedCount, 1));

        var select = component.Find("[data-testid='BulkAssignSelect']");
        select.GetAttribute("autocomplete").ShouldBe("off");
    }

    [Test]
    public void BulkChangeStatusSelect_HasAutocompleteOff()
    {
        using var ctx = new BunitContext();
        var component = ctx.Render<WorkOrderBulkActionBar>(p => p
            .Add(c => c.SelectedCount, 1));

        var select = component.Find("[data-testid='BulkChangeStatusSelect']");
        select.GetAttribute("autocomplete").ShouldBe("off");
    }
}
