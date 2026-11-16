using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class PrintLinkTests
{
    [Test]
    public async Task Should_RenderPrintLink_WithRelNoopenerNoreferrer()
    {
        var creator = WorkOrderManageSteps.Viewer();
        var order = WorkOrderBuilder.Create()
            .WithId(Guid.NewGuid()).WithNumber("WO-PRINT").WithTitle("Print link").WithDescription("Print link")
            .WithStatus(WorkOrderStatus.Draft).CreatedBy(creator).CreatedAt(DateTime.UtcNow).Build();
        var page = WorkOrderManagePageContext.Create().SignedInAs(creator).Editing(order);
        await using var ctx = page.Build();

        var link = await page.Render(ctx)
            .WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.PrintLink));

        link.GetAttribute("href").ShouldBe("/workorder/print/WO-PRINT");
        link.GetAttribute("target").ShouldBe("_blank");
        link.GetAttribute("rel").ShouldBe("noopener noreferrer");
    }

    [Test]
    public async Task Should_RenderPrintLink_WithVisuallyHiddenNewTabText()
    {
        var creator = WorkOrderManageSteps.Viewer();
        var order = WorkOrderBuilder.Create()
            .WithId(Guid.NewGuid()).WithNumber("WO-PRINT").WithTitle("Print link").WithDescription("Print link")
            .WithStatus(WorkOrderStatus.Draft).CreatedBy(creator).CreatedAt(DateTime.UtcNow).Build();
        var page = WorkOrderManagePageContext.Create().SignedInAs(creator).Editing(order);
        await using var ctx = page.Build();

        var link = await page.Render(ctx)
            .WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.PrintLink));

        link.TextContent.ShouldContain("Print");
        var printIndex = link.TextContent.IndexOf("Print", StringComparison.Ordinal);
        var hintIndex = link.TextContent.IndexOf("(opens in a new tab)", StringComparison.Ordinal);
        hintIndex.ShouldBeGreaterThan(printIndex);
        var hidden = link.QuerySelector(".visually-hidden");
        hidden.ShouldNotBeNull();
        hidden.TextContent.ShouldBe("(opens in a new tab)");
    }
}
