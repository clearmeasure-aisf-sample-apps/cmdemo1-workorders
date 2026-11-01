using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.Playwright;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderManageCopyButtonTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldRenderCopyButton_WhenViewingExistingWorkOrder()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await ClickWorkOrderNumberFromSearchPage(order);

        var copyButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CopyWorkOrderNumber));
        await Expect(copyButton).ToBeVisibleAsync();
        await Expect(copyButton).ToHaveAttributeAsync("aria-label", "Copy work order number");
    }

    [Test, Retry(2)]
    public async Task ShouldCopyWorkOrderNumber_ToClipboard()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await ClickWorkOrderNumberFromSearchPage(order);

        var copyButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CopyWorkOrderNumber));
        await Expect(copyButton).ToBeVisibleAsync();

        await Click(nameof(WorkOrderManage.Elements.CopyWorkOrderNumber));
        await Task.Delay(500); // Allow JS to execute

        var clipboardText = await Page.EvaluateAsync<string>("navigator.clipboard.readText()");
        clipboardText.ShouldBe(order.Number);
    }

    [Test, Retry(2)]
    public async Task ShouldShowCheckIconAndAriaLiveMessage_OnCopySuccess()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await ClickWorkOrderNumberFromSearchPage(order);

        var copyButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CopyWorkOrderNumber));
        await Expect(copyButton).ToBeVisibleAsync();

        await Click(nameof(WorkOrderManage.Elements.CopyWorkOrderNumber));

        // Check that icon changed to check2
        var icon = copyButton.Locator("i");
        await Expect(icon).ToHaveClassAsync(new Regex("bi-check2"));

        // Check aria-live message
        var statusRegion = Page.Locator("[role='status'][aria-live='polite']");
        await Expect(statusRegion).ToContainTextAsync("Copied");

        // Wait for revert (2 seconds)
        await Task.Delay(2500);

        // Icon should revert to clipboard
        await Expect(icon).ToHaveClassAsync(new Regex("bi-clipboard"));
    }

    [Test, Retry(2)]
    public async Task ShouldNotRenderCopyButton_WhenCreatingNewWorkOrder()
    {
        await LoginAsCurrentUser();

        await Page.GotoAsync("/workorder/manage?mode=new");
        await WaitForNewWorkOrderFormReadyAsync();

        var copyButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CopyWorkOrderNumber));
        await Expect(copyButton).ToHaveCountAsync(0);
    }
}
