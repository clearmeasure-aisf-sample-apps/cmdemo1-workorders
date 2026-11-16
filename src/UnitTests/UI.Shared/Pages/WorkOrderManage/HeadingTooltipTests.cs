using Bunit;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class HeadingTooltipTests
{
    [Test]
    public async Task WorkOrderManage_ShouldExposeManageHeadingTooltip()
    {
        var creator = WorkOrderManageSteps.Viewer();
        var page = WorkOrderManagePageContext.Create()
            .SignedInAs(creator)
            .WithNewOrder(o => o.WithId(Guid.NewGuid()).WithNumber("WO-TIP").WithTitle("Heading tooltip"));
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        var element = await component.WaitForElementAsync(
            WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.ManageHeadingTooltip));
        element.GetAttribute("title").ShouldBe("Create or edit a work order");
    }
}
