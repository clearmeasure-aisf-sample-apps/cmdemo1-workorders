using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class TitleAutocompleteTests
{
    [Test]
    public async Task TitleInput_HasAutocompleteOff_ToPreventBrowserAutofill()
    {
        var page = NewModePage();
        await using var ctx = page.Build();

        var input = await page.Render(ctx)
            .WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Title));

        input.GetAttribute("autocomplete").ShouldBe("off");
    }

    private static WorkOrderManagePageContext NewModePage()
    {
        var creator = WorkOrderManageSteps.Viewer();
        return WorkOrderManagePageContext.Create()
            .SignedInAs(creator)
            .WithNewOrder(o => o
                .WithId(Guid.NewGuid())
                .WithNumber("WO-AUTO")
                .WithTitle("Autocomplete test")
                .WithDescription("Test description"));
    }
}
