using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSearchLongRoomTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldNotWidthResultsTableWithLongRoom()
    {
        await LoginAsCurrentUser();

        var room = new string('R', WorkOrder.RoomNumberMaxLength);
        var order = Faker<WorkOrder>();
        order.Title = $"[{TestTag}] long room search";
        order.Number = null;
        order.RoomNumber = room;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        order.Number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), room);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
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
    }
}
