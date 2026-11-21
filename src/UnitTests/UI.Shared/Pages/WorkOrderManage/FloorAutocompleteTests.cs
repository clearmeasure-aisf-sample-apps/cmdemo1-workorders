using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class FloorAutocompleteTests
{
    [Test]
    public async Task FloorInput_HasAutocompleteOff()
    {
        var page = NewModePage();
        await using var ctx = page.Build();

        var input = await page.Render(ctx)
            .WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Floor));

        input.GetAttribute("autocomplete").ShouldBe("off");
    }

    private static WorkOrderManagePageContext NewModePage()
    {
        var creator = WorkOrderManageSteps.Viewer();
        return WorkOrderManagePageContext.Create()
            .SignedInAs(creator)
            .WithNewOrder(o => o
                .WithId(Guid.NewGuid())
                .WithNumber("WO-FLOOR")
                .WithTitle("Floor autocomplete test")
                .WithDescription("Test floor autocomplete off"));
    }
}
