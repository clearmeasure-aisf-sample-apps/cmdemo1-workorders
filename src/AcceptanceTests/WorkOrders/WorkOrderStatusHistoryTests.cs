using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.Playwright;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[TestFixture]
public class WorkOrderStatusHistoryTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task WorkOrderStatusHistory_ShouldRecordTransitionsAndDisplayInHistory()
    {
        await LoginAsCurrentUser();

        // Create a new work order in Draft status
        var workOrder = await CreateAndSaveNewWorkOrder();

        // Navigate to the work order manage page
        await Click(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify that initially there are no status changes
        var historySection = Page.GetByTestId(nameof(WorkOrderManage.Elements.StatusHistory));
        await Expect(historySection).ToContainTextAsync("No status changes recorded.");

        // Assign the work order (Draft -> Assigned)
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Input(nameof(WorkOrderManage.Elements.Title), "Test Title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test Description");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate back to the work order
        await ClickWorkOrderNumberFromSearchPage(workOrder);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify the history section shows the transition
        var historyRows = Page.GetByTestId(nameof(WorkOrderManage.Elements.StatusHistoryRow));
        var rowCount = await historyRows.CountAsync();
        rowCount.ShouldBeGreaterThanOrEqualTo(1);

        // Verify the first entry shows Draft -> Assigned
        var toStatusCell = Page.GetByTestId(nameof(WorkOrderManage.Elements.StatusHistoryToStatus)).First;
        await Expect(toStatusCell).ToContainTextAsync("Assigned");

        var fromStatusCell = Page.GetByTestId(nameof(WorkOrderManage.Elements.StatusHistoryFromStatus)).First;
        await Expect(fromStatusCell).ToContainTextAsync("Draft");

        // Transition to InProgress
        var inProgressButton = Page.GetByText("In Progress");
        if (await inProgressButton.IsVisibleAsync())
        {
            await inProgressButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Navigate back to the work order
            await ClickWorkOrderNumberFromSearchPage(workOrder);
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify we now have 2 entries in the history
            var updatedRowCount = await historyRows.CountAsync();
            updatedRowCount.ShouldBeGreaterThanOrEqualTo(2);

            // Verify the first entry is now InProgress (newest first)
            var firstToStatusCell = Page.GetByTestId(nameof(WorkOrderManage.Elements.StatusHistoryToStatus)).First;
            await Expect(firstToStatusCell).ToContainTextAsync("InProgress");
        }
    }
}
