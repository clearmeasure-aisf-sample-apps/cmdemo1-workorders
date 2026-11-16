using Bunit;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class RequiredFieldsNoteTests
{
    [Test]
    public async Task WorkOrderManage_ShouldShowRequiredFieldsNote()
    {
        var creator = WorkOrderManageSteps.Viewer();
        var page = WorkOrderManagePageContext.Create()
            .SignedInAs(creator)
            .WithNewOrder(o => o.WithId(Guid.NewGuid()).WithNumber("WO-REQ").WithTitle("Required fields note"));
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        var element = await component.WaitForElementAsync(
            WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.RequiredFieldsNote));
        element.TextContent.Trim().ShouldBe("Fields marked with * are required.");
    }
}
