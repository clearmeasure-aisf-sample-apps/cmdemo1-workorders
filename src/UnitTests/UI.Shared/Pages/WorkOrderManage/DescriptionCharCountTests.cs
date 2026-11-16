using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class DescriptionCharCountTests
{
    [Test]
    public async Task DescriptionCharCount_ShowsFullLimit_WhenDescriptionIsEmpty()
    {
        var page = NewModePage(description: "");
        await using var ctx = page.Build();

        var caption = await page.Render(ctx)
            .WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DescriptionCharCount));

        caption.TextContent.Trim().ShouldBe("4000 characters remaining");
        caption.ClassList.ShouldContain("char-count");
    }

    [Test]
    public async Task DescriptionCharCount_Decrements_AsDescriptionLengthGrows()
    {
        var page = NewModePage(description: new string('A', 10));
        await using var ctx = page.Build();

        var caption = await page.Render(ctx)
            .WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DescriptionCharCount));

        caption.TextContent.Trim().ShouldBe("3990 characters remaining");
    }

    [Test]
    public async Task DescriptionCharCount_ShowsDanger_WhenAtLimit()
    {
        var page = NewModePage(description: new string('X', WorkOrder.DescriptionMaxLength));
        await using var ctx = page.Build();

        var caption = await page.Render(ctx)
            .WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DescriptionCharCount));

        caption.TextContent.Trim().ShouldBe("0 characters remaining");
        caption.ClassList.ShouldContain("text-danger");
    }

    [Test]
    public async Task DescriptionCharCount_NotRendered_WhenReadOnly()
    {
        var page = ReadOnlyCompletedPage();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Description));

        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DescriptionCharCount))
            .ShouldBeEmpty();
    }

    [Test]
    public async Task DescriptionTextarea_HasMaxLengthAttribute_EqualToDescriptionMaxLength()
    {
        var page = NewModePage(description: "");
        await using var ctx = page.Build();

        var textarea = await page.Render(ctx)
            .WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Description));

        textarea.GetAttribute("maxlength").ShouldBe(WorkOrder.DescriptionMaxLength.ToString());
    }

    private static WorkOrderManagePageContext NewModePage(string description)
    {
        var creator = WorkOrderManageSteps.Viewer();
        return WorkOrderManagePageContext.Create()
            .SignedInAs(creator)
            .WithNewOrder(o => o
                .WithId(Guid.NewGuid())
                .WithNumber("WO-DESC")
                .WithTitle("Char count test")
                .WithDescription(description));
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
