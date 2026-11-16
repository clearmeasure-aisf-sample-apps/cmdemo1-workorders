using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class TitleCharCountTests
{
    [Test]
    public async Task TitleCharCount_ShowsFullLimit_WhenTitleIsEmpty()
    {
        var page = NewModePage(title: "");
        await using var ctx = page.Build();

        var caption = await page.Render(ctx)
            .WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.TitleCharCount));

        caption.TextContent.Trim().ShouldBe("600 characters remaining");
        caption.ClassList.ShouldContain("char-count");
    }

    [Test]
    public async Task TitleCharCount_Decrements_AsTitleLengthGrows()
    {
        var page = NewModePage(title: new string('A', 10));
        await using var ctx = page.Build();

        var caption = await page.Render(ctx)
            .WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.TitleCharCount));

        caption.TextContent.Trim().ShouldBe($"{WorkOrder.TitleMaxLength - 10} characters remaining");
    }

    [Test]
    public async Task TitleCharCount_ShowsDanger_WhenAtLimit()
    {
        var page = NewModePage(title: new string('X', WorkOrder.TitleMaxLength));
        await using var ctx = page.Build();

        var caption = await page.Render(ctx)
            .WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.TitleCharCount));

        caption.TextContent.Trim().ShouldBe("0 characters remaining");
        caption.ClassList.ShouldContain("text-danger");
    }

    [Test]
    public async Task TitleCharCount_NotRendered_WhenReadOnly()
    {
        var page = ReadOnlyCompletedPage();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Title));

        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.TitleCharCount))
            .ShouldBeEmpty();
    }

    [Test]
    public async Task TitleInput_HasMaxLengthAttribute_EqualToTitleMaxLength()
    {
        var page = NewModePage(title: "");
        await using var ctx = page.Build();

        var input = await page.Render(ctx)
            .WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Title));

        input.GetAttribute("maxlength").ShouldBe(WorkOrder.TitleMaxLength.ToString());
    }

    private static WorkOrderManagePageContext NewModePage(string title)
    {
        var creator = WorkOrderManageSteps.Viewer();
        return WorkOrderManagePageContext.Create()
            .SignedInAs(creator)
            .WithNewOrder(o => o
                .WithId(Guid.NewGuid())
                .WithNumber("WO-TITLE")
                .WithTitle(title)
                .WithDescription("Char count test"));
    }

    private static WorkOrderManagePageContext ReadOnlyCompletedPage()
    {
        var viewer = EmployeeBuilder.Create("viewer")
            .WithFirstName("View").WithLastName("Only").WithEmail("viewer@example.com").WithId(Guid.NewGuid()).Build();
        var creator = EmployeeBuilder.Create("creator")
            .WithFirstName("Create").WithLastName("Or").WithEmail("creator@example.com").WithId(Guid.NewGuid()).Build();
        var completed = WorkOrderBuilder.Create()
            .WithId(Guid.NewGuid()).WithNumber("WO-RO").WithTitle("Completed").WithDescription("Done")
            .WithStatus(WorkOrderStatus.Complete).CreatedBy(creator).AssignedTo(creator).Build();
        return WorkOrderManagePageContext.Create().SignedInAs(viewer).Editing(completed);
    }
}
