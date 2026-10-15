using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderPrintTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldOpenPrintViewFromManagePage()
    {
        await LoginAsCurrentUser();
        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);

        var popup = await Page.RunAndWaitForPopupAsync(async () =>
            await Page.GetByTestId(nameof(WorkOrderManage.Elements.PrintLink)).ClickAsync());

        await Expect(popup.GetByTestId(nameof(WorkOrderPrint.Elements.Number))).ToHaveTextAsync(order.Number!);
        await Expect(popup.GetByTestId(nameof(WorkOrderPrint.Elements.Title))).ToHaveTextAsync(order.Title!);
        await Expect(popup.Locator("#app-navigation-rail")).ToHaveCountAsync(0);
    }
}
