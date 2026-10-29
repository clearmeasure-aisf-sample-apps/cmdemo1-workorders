using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[TestFixture]
public class WorkOrderStatusHistoryTests : AcceptanceTestBase
{
	[Test, Retry(2)]
	public async Task WorkOrderStatusHistory_ShouldRecordTransitionsAndDisplayInHistory()
	{
		await LoginAsCurrentUser();

		var order = await CreateAndSaveNewWorkOrder();
		order = await ClickWorkOrderNumberFromSearchPage(order);

		await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.StatusHistory)).First)
			.ToContainTextAsync("No status changes recorded.");

		order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
		order = await ClickWorkOrderNumberFromSearchPage(order);

		await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.StatusHistoryRow))).ToHaveCountAsync(1);
		await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.StatusHistoryFromStatus)).First)
			.ToContainTextAsync(WorkOrderStatus.Draft.FriendlyName);
		await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.StatusHistoryToStatus)).First)
			.ToContainTextAsync(WorkOrderStatus.Assigned.FriendlyName);

		await Click(nameof(WorkOrderManage.Elements.CommandButton) + AssignedToInProgressCommand.Name);
		await ClickWorkOrderNumberFromSearchPage(order);

		await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.StatusHistoryRow))).ToHaveCountAsync(2);
		await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.StatusHistoryToStatus)).First)
			.ToContainTextAsync(WorkOrderStatus.InProgress.FriendlyName);
	}
}
