using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.AspNetCore.Components.Web;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class WorkOrderBulkActionBarTests
{
    [Test]
    public async Task ShouldRaiseAssignAndChangeStatusWithChosenValues()
    {
        await using var ctx = new BunitContext();
        string? assigned = null;
        string? verb = null;
        var component = ctx.Render<WorkOrderBulkActionBar>(p => p
            .Add(c => c.SelectedCount, 2)
            .Add(c => c.UserOptions, [new SelectListItem("gwillie", "Groundskeeper Willie")])
            .Add(c => c.StatusOptions, ["Begin", "Cancel"])
            .Add(c => c.OnAssign, value => assigned = value)
            .Add(c => c.OnChangeStatus, value => verb = value));

        component.Find("[data-testid='BulkSelectedCount']").TextContent.ShouldBe("2 selected");
        component.Find("[data-testid='BulkAssignButton']").HasAttribute("disabled").ShouldBeTrue();
        await component.Find("[data-testid='BulkAssignSelect']").ChangeAsync(new() { Value = "gwillie" });
        await component.Find("[data-testid='BulkAssignButton']").ClickAsync(new());
        await component.Find("[data-testid='BulkChangeStatusSelect']").ChangeAsync(new() { Value = "Cancel" });
        await component.Find("[data-testid='BulkChangeStatusButton']").ClickAsync(new());

        assigned.ShouldBe("gwillie");
        verb.ShouldBe("Cancel");
        component.FindAll("[data-testid='BulkChangeStatusSelect'] option").Select(o => o.TextContent)
            .ShouldBe(["Change status…", "Begin", "Cancel"]);
    }

    [Test]
    public async Task MoreMenu_ExportsClearsAndClosesOnEscape()
    {
        await using var ctx = new BunitContext();
        var exported = false;
        var cleared = false;
        var component = ctx.Render<WorkOrderBulkActionBar>(p => p
            .Add(c => c.SelectedCount, 1)
            .Add(c => c.OnExportSelected, () => exported = true)
            .Add(c => c.OnClearSelection, () => cleared = true));
        var more = "[data-testid='BulkMoreButton']";

        await component.Find(more).ClickAsync(new());
        component.Find(".bulk-menu").HasAttribute("hidden").ShouldBeFalse();
        await component.Find("[data-testid='BulkExportSelected']").ClickAsync(new());
        await component.Find(more).ClickAsync(new());
        await component.Find("[data-testid='BulkClearSelection']").ClickAsync(new());
        await component.Find(more).ClickAsync(new());
        await component.Find(".bulk-bar").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        exported.ShouldBeTrue();
        cleared.ShouldBeTrue();
        component.Find(".bulk-menu").HasAttribute("hidden").ShouldBeTrue();
        component.Find(more).GetAttribute("aria-expanded").ShouldBe("false");
        ctx.JSInterop.VerifyFocusAsyncInvoke();
    }
}
