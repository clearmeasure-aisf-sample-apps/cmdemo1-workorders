using System.Globalization;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[Parallelizable(ParallelScope.All)]
public class WorkOrderPrintTests : WorkOrderAcceptanceTestBase
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

        var yearText = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        var footer = popup.Locator("footer.print-copyright");
        await Expect(footer).ToContainTextAsync(yearText);
        await Expect(footer).ToContainTextAsync("ClearMeasure Labs");
        await Expect(footer).ToContainTextAsync("· Facilities");
    }
}
