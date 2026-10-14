using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderWaitingOnVendorTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldTransitionToWaitingOnVendorAndShowPurplePill()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        // Click "Wait on vendor" command button
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + InProgressToWaitingOnVendorCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify status changed to WaitingOnVendor
        var rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ??
                             throw new InvalidOperationException();
        rehyratedOrder.Status.ShouldBe(WorkOrderStatus.WaitingOnVendor);

        // Reload the page and verify the purple pill is displayed
        order = await ClickWorkOrderNumberFromSearchPage(rehyratedOrder);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.WaitingOnVendor.FriendlyName);

        // Verify the status badge has the waiting CSS class
        var statusElement = Page.GetByTestId(nameof(WorkOrderManage.Elements.Status));
        await Expect(statusElement).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("status-badge--waiting"));
    }

    [Test, Retry(2)]
    public async Task ShouldResumeFromWaitingOnVendorToInProgress()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        // Transition to WaitingOnVendor
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + InProgressToWaitingOnVendorCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        order = await ClickWorkOrderNumberFromSearchPage(order);

        // Click "Resume" command button to go back to InProgress
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + WaitingOnVendorToInProgressCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify status changed back to InProgress
        var rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ??
                             throw new InvalidOperationException();
        rehyratedOrder.Status.ShouldBe(WorkOrderStatus.InProgress);

        // Reload and verify InProgress pill is displayed
        order = await ClickWorkOrderNumberFromSearchPage(rehyratedOrder);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.InProgress.FriendlyName);
    }

    [Test, Retry(2)]
    public async Task ShouldDisplayWaitingOnVendorInSearchResults()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        // Transition to WaitingOnVendor
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + InProgressToWaitingOnVendorCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate back to search to verify the WaitingOnVendor column shows the order with purple pill
        await Click(nameof(WorkOrderSearch.Elements.SearchHeadingTooltip));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Look for the WaitingOnVendor column
        var wtvColumnHeader = Page.Locator($"[data-testid='{nameof(WorkOrderSearch.Elements.BoardColumnCount)}WaitingOnVendor']");
        await Expect(wtvColumnHeader).ToBeVisibleAsync();

        // Verify the work order appears in the results
        var orderNumber = Page.Locator($"text='{order.Number}'");
        await Expect(orderNumber).ToBeVisibleAsync();

        // Verify status badge shows the purple pill
        var statusPill = Page.Locator("text='Waiting on vendor'").First;
        await Expect(statusPill).ToBeVisibleAsync();
    }
}
