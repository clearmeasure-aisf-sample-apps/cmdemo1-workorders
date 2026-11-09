using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderRoomNumberLengthTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldSaveWorkOrderWithRoomNumberAtEntryMaxLength()
    {
        await LoginAsCurrentUser();

        var room = new string('R', WorkOrder.RoomNumberEntryMaxLength);
        var order = Faker<WorkOrder>();
        order.Title = $"[{TestTag}] 50 char room";
        order.Number = null;
        order.RoomNumber = room;

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
        await ClickWorkOrderNumberFromSearchPage(order);

        var roomField = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));
        await Expect(roomField).ToHaveValueAsync(room);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!))
            ?? throw new InvalidOperationException();
        rehydratedOrder.RoomNumber.ShouldBe(room);
        rehydratedOrder.RoomNumber!.Length.ShouldBe(WorkOrder.RoomNumberEntryMaxLength);
    }

    [Test, Retry(2)]
    public async Task ShouldAllowOpeningAndSavingWorkOrderWithLongRoomNumberWhenUnchanged()
    {
        await LoginAsCurrentUser();
        var order = await CreateAndSaveNewWorkOrder();

        // Seed a legacy room number longer than the entry limit, as stored before the 50-character rule.
        var longRoom = new string('R', WorkOrder.RoomNumberMaxLength);
        var stored = (await Bus.Send(new WorkOrderByNumberQuery(order.Number!)))!;
        stored.RoomNumber = longRoom;
        await Bus.Send(new SaveDraftCommand(stored, stored.Creator!) { PreviousRoomNumber = longRoom });

        await ClickWorkOrderNumberFromSearchPage(order);

        var roomField = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));
        await Expect(roomField).ToHaveValueAsync(longRoom);

        var newTitle = $"[{TestTag}] 900 char room unchanged";
        await Input(nameof(WorkOrderManage.Elements.Title), newTitle);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!))
            ?? throw new InvalidOperationException();
        rehydratedOrder.RoomNumber.ShouldBe(longRoom);
        rehydratedOrder.RoomNumber!.Length.ShouldBe(WorkOrder.RoomNumberMaxLength);
        rehydratedOrder.Title.ShouldBe(newTitle);
    }

    [Test, Retry(2)]
    public async Task ShouldRejectRoomLongerThan50Characters()
    {
        await LoginAsCurrentUser();

        var tooLong = new string('X', WorkOrder.RoomNumberEntryMaxLength + 1);

        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] 51 char room");
        await Input(nameof(WorkOrderManage.Elements.Description), "description");

        var roomField = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));
        await Expect(roomField).ToBeEditableAsync(new LocatorAssertionsToBeEditableOptions { Timeout = 30_000 });
        await roomField.EvaluateAsync("el => el.removeAttribute('maxlength')");
        await roomField.FillAsync(tooLong);
        await roomField.BlurAsync();

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        await Expect(Page).ToHaveURLAsync(new Regex("workorder/manage"));
        await Expect(Page.GetByText("Room number cannot exceed 50 characters.")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });

        WorkOrder? stored = await Bus.Send(new WorkOrderByNumberQuery(number));
        stored.ShouldBeNull();
    }
}
