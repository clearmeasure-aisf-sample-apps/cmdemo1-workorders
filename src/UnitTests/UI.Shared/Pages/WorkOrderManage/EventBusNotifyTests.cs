using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class EventBusNotifyTests
{
    [Test]
    public void ShouldCountNonGenericNotify_WhenNotifiedThroughTheObjectOverload()
    {
        var uiBus = new WorkOrderManageUiBusSpy();
        var workOrder = new WorkOrder { Id = Guid.NewGuid(), Number = "WO-TEST" };

        uiBus.Notify((object)new WorkOrderSelectedEvent(workOrder));

        uiBus.NonGenericNotifyCount.ShouldBe(1);
        uiBus.NotifiedWorkOrderSelectedCount.ShouldBe(1);
    }

    [Test]
    public async Task ShouldNotRenotifyWorkOrderSelectedOnEveryRerender_WhenWorkOrderIsUnchanged()
    {
        var uiBus = new WorkOrderManageUiBusSpy();
        var creator = WorkOrderManageSteps.Viewer();
        var page = WorkOrderManagePageContext.Create()
            .SignedInAs(creator)
            .WithUiBus(uiBus)
            .WithNewOrder(o => o.WithId(Guid.NewGuid()).WithNumber("WO-TEST").WithTitle("Test Order"));
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() => uiBus.NotifiedWorkOrderSelectedCount.ShouldBe(1));
        var countAfterInitialLoad = uiBus.NotifiedWorkOrderSelectedCount;
        var titleInput = component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Title));
        await titleInput.ChangeAsync(new() { Value = "First edit forces a re-render" });
        await titleInput.ChangeAsync(new() { Value = "Second edit forces another re-render" });

        uiBus.NotifiedWorkOrderSelectedCount.ShouldBe(countAfterInitialLoad,
            "typing into the Title field re-renders the page but must not re-notify " +
            "WorkOrderSelectedEvent for the SAME work order instance on every render; " +
            "that unconditional notify forces listeners like WorkOrderChat to reset their own state.");
    }
}
