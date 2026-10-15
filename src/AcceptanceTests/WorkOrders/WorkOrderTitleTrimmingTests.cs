using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderTitleTrimmingTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldTrimSurroundingSpacesFromTitle()
    {
        await LoginAsCurrentUser();

        var titleWithSpaces = "  Fix the sink  ";
        var expectedTrimmedTitle = "Fix the sink";
        var order = Faker<WorkOrder>();
        order.Title = titleWithSpaces;
        order.Number = null;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        order.Number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), titleWithSpaces);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await ClickWorkOrderNumberFromSearchPage(order);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(expectedTrimmedTitle);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!))
            ?? throw new InvalidOperationException();
        rehydratedOrder.Title.ShouldBe(expectedTrimmedTitle);
    }

    [Test, Retry(2)]
    public async Task ShouldRejectWhitespaceOnlyTitle()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "   ");
        await Input(nameof(WorkOrderManage.Elements.Description), "description");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("workorder/manage"));
        await Expect(Page.GetByText("Title is required.")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });

        WorkOrder? stored = await Bus.Send(new WorkOrderByNumberQuery(number));
        stored.ShouldBeNull();
    }
}
