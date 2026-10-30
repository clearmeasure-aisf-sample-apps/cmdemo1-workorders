using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderWaitingOnVendorTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShowWaitingPillThenInProgressPillAfterResume()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + InProgressToWaitingOnVendorCommand.Name);
        await SearchForWorkOrderAsync(order.Number!);

        var cell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.StatusCell) + order.Number);
        await Expect(cell.Locator(".status-badge--waiting")).ToHaveTextAsync("Waiting on vendor");
        var waiting = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        waiting.Status.ShouldBe(WorkOrderStatus.InProgress);
        waiting.WaitingOnVendorSince.ShouldNotBeNull();

        order = await ClickWorkOrderNumberFromSearchPage(order);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + WaitingOnVendorToInProgressCommand.Name);
        await SearchForWorkOrderAsync(order.Number!);

        await Expect(cell.Locator(".status-badge--in-progress")).ToHaveTextAsync(WorkOrderStatus.InProgress.FriendlyName);
        await Expect(cell.Locator(".status-badge--waiting")).ToHaveCountAsync(0);
        var resumed = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        resumed.Status.ShouldBe(WorkOrderStatus.InProgress);
        resumed.WaitingOnVendorSince.ShouldBeNull();
    }

    /// <summary>
    /// Waits for the command's navigation to the search page, then searches for the number so the row
    /// is on screen even when parallel tests have pushed it off page 1.
    /// </summary>
    private async Task SearchForWorkOrderAsync(string number)
    {
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        var searchTable = Page.Locator(".grid-data tbody");
        await searchTable.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await Page.EvaluateAsync("u => Blazor.navigateTo(u)", $"/workorder/search?Q={Uri.EscapeDataString(number)}");
        await Page.GetByTestId(nameof(WorkOrderSearch.Elements.StatusCell) + number)
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
    }
}
