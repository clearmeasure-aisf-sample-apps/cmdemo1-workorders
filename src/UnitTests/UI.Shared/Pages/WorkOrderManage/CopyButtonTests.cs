using Bunit;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class CopyButtonTests
{
    [Test]
    public async Task WorkOrderManage_ShouldRenderCopyButton_WhenExistingWorkOrder()
    {
        var workOrderId = Guid.NewGuid();
        var clipboard = new WorkOrderManageClipboard();
        var page = CopyPage(workOrderId, "WO-NEW", clipboard)
            .NavigateTo($"/workorder/manage/{workOrderId}");
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        var copyButton = await component.WaitForElementAsync(
            WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.CopyWorkOrderNumber));
        copyButton.ShouldNotBeNull();
        copyButton.GetAttribute("aria-label").ShouldBe("Copy work order number WO-NEW");
        copyButton.TextContent.ShouldContain("Copy");
    }

    [Test]
    public async Task WorkOrderManage_ShouldNotRenderCopyButton_WhenNewWorkOrder()
    {
        var workOrderId = Guid.NewGuid();
        var page = WorkOrderManagePageContext.Create()
            .SignedInAs(WorkOrderManageSteps.Viewer())
            .WithJsRuntime(new WorkOrderManageClipboard())
            .WithNewOrder(o => o.WithId(workOrderId).WithNumber("WO-NEW").WithTitle("New Work Order").AsBrandNew());
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        try
        {
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.CopyWorkOrderNumber));
            Assert.Fail("Expected no copy button for new work order, but one was found");
        }
        catch (ElementNotFoundException)
        {
        }
    }

    [Test]
    public async Task CopyWorkOrderNumberAsync_ShouldExecuteSuccessfully_WhenClickingCopyButton()
    {
        var workOrderId = Guid.NewGuid();
        var clipboard = new WorkOrderManageClipboard();
        var page = CopyPage(workOrderId, "WO-456", clipboard)
            .NavigateTo($"/workorder/manage/{workOrderId}");
        await using var ctx = page.Build();
        var component = page.Render(ctx);
        var copyButton = await component.WaitForElementAsync(
            WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.CopyWorkOrderNumber));
        copyButton.ShouldNotBeNull();

        var click = copyButton.ClickAsync(new());
        await component.WaitForAssertionAsync(() =>
        {
            clipboard.LastCopiedText.ShouldBe("WO-456");
            component.Find("#copy-status").TextContent.ShouldContain("Copied");
        });
        await click;
    }

    [Test]
    public async Task CopyWorkOrderNumberAsync_ShouldHandleJSException_Gracefully()
    {
        var workOrderId = Guid.NewGuid();
        var page = CopyPage(workOrderId, "WO-NEW", new WorkOrderManageClipboard(throwJsException: true))
            .NavigateTo($"/workorder/manage/{workOrderId}");
        await using var ctx = page.Build();
        var component = page.Render(ctx);
        var copyButton = await component.WaitForElementAsync(
            WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.CopyWorkOrderNumber));

        await copyButton.ClickAsync(new());

        component.Find("#copy-status").TextContent.ShouldNotContain("Copied");
    }

    [Test]
    public async Task CopyWorkOrderNumberAsync_ShouldHandleJSDisconnectedException_Gracefully()
    {
        var workOrderId = Guid.NewGuid();
        var page = CopyPage(workOrderId, "WO-NEW",
                new WorkOrderManageClipboard(throwDisconnectedException: true))
            .NavigateTo($"/workorder/manage/{workOrderId}");
        await using var ctx = page.Build();
        var component = page.Render(ctx);
        var copyButton = await component.WaitForElementAsync(
            WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.CopyWorkOrderNumber));

        await copyButton.ClickAsync(new());

        component.Find("#copy-status").TextContent.ShouldNotContain("Copied");
    }

    private static WorkOrderManagePageContext CopyPage(
        Guid workOrderId, string number, WorkOrderManageClipboard clipboard) =>
        WorkOrderManagePageContext.Create()
            .SignedInAs(WorkOrderManageSteps.Viewer())
            .WithJsRuntime(clipboard)
            .WithNewOrder(o => o.WithId(workOrderId).WithNumber(number).WithTitle("Test Work Order"));
}
