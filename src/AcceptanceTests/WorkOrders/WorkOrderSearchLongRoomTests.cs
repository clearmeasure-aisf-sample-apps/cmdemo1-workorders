using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSearchLongRoomTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldNotWidenResultsTableWithLongRoom()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        // The form limits entry to RoomNumberEntryMaxLength; seed a legacy room up to the column length directly.
        var room = new string('R', WorkOrder.RoomNumberMaxLength);
        var stored = (await Bus.Send(new WorkOrderByNumberQuery(order.Number!)))!;
        stored.RoomNumber = room;
        await Bus.Send(new SaveDraftCommand(stored, stored.Creator!) { PreviousRoomNumber = room });

        await Page.ReloadAsync();
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        await Page.SetViewportSizeAsync(1280, 800);

        var roomCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.RoomCell) + order.Number);
        await Expect(roomCell).ToBeVisibleAsync();

        var isNotScrolled = await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= document.documentElement.clientWidth");
        isNotScrolled.ShouldBeTrue();

        var roomText = roomCell.Locator(".room-text");
        var titleAttribute = await roomText.GetAttributeAsync("title");
        titleAttribute.ShouldBe(room);

        // .table-scroll has overflow-x:auto, so the page check alone would pass without the fix.
        var roomTextBox = await roomText.BoundingBoxAsync();
        roomTextBox.ShouldNotBeNull();
        roomTextBox.Width.ShouldBeLessThanOrEqualTo(141);

        var cellBox = await roomCell.BoundingBoxAsync();
        cellBox.ShouldNotBeNull();
        cellBox.Width.ShouldBeLessThanOrEqualTo(200);

        var tableWidth = await Page.Locator(".grid-data").EvaluateAsync<double>("t => t.getBoundingClientRect().width");
        tableWidth.ShouldBeLessThanOrEqualTo(1280);

        var isTruncated = await roomText.EvaluateAsync<bool>("e => e.scrollWidth > e.clientWidth");
        isTruncated.ShouldBeTrue();
    }
}
