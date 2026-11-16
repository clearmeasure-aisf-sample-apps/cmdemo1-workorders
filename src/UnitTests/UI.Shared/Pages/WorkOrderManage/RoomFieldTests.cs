using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class RoomFieldTests
{
    [Test]
    public async Task WorkOrderManage_ShouldRenderRoomAsTextareaWithMaxLength50()
    {
        var creator = WorkOrderManageSteps.Viewer();
        var page = WorkOrderManagePageContext.Create()
            .SignedInAs(creator)
            .WithNewOrder(o => o.WithId(Guid.NewGuid()).WithNumber("WO-ROOM").WithTitle("Room field"));
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        var room = await component.WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.RoomNumber));
        room.TagName.ShouldBe("TEXTAREA");
        room.GetAttribute("maxlength").ShouldBe(WorkOrder.RoomNumberEntryMaxLength.ToString());
    }
}
