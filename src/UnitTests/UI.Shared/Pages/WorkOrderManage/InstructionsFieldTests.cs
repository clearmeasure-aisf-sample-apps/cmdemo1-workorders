using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class InstructionsFieldTests
{
    [Test]
    public async Task WorkOrderManage_ShouldRenderInstructionsAsTextareaWithMaxLength4000()
    {
        var page = NewModePage();
        await using var ctx = page.Build();

        var instructions = await page.Render(ctx)
            .WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Instructions));

        instructions.TagName.ShouldBe("TEXTAREA");
        instructions.GetAttribute("maxlength").ShouldBe(WorkOrder.InstructionsMaxLength.ToString());
    }

    [Test]
    public async Task WorkOrderManage_ShouldPlaceInstructionsBetweenDescriptionAndRoom()
    {
        var page = NewModePage();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Instructions));

        var markup = component.Markup;
        var descriptionIndex = markup.IndexOf($"data-testid=\"{WorkOrderManagePage.Elements.Description}\"", StringComparison.Ordinal);
        var instructionsIndex = markup.IndexOf($"data-testid=\"{WorkOrderManagePage.Elements.Instructions}\"", StringComparison.Ordinal);
        var roomIndex = markup.IndexOf($"data-testid=\"{WorkOrderManagePage.Elements.RoomNumber}\"", StringComparison.Ordinal);
        descriptionIndex.ShouldBeGreaterThanOrEqualTo(0);
        instructionsIndex.ShouldBeGreaterThan(descriptionIndex);
        roomIndex.ShouldBeGreaterThan(instructionsIndex);
    }

    [Test]
    public async Task WorkOrderManage_ShouldShowRisksLabelForInstructionsField()
    {
        var page = NewModePage();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Instructions));

        component.Markup.ShouldContain(">Risks</label>");
        component.Markup.ShouldNotContain(">Instructions</label>");
    }

    [Test]
    public async Task WorkOrderManage_ShouldDisableInstructionsWhenReadOnly()
    {
        var page = ReadOnlyCompletedPage();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        var instructions = await component.WaitForElementAsync(
            WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Instructions));

        instructions.HasAttribute("disabled").ShouldBeTrue();
        component.Instance.Model.Instructions.ShouldBe("Saved guidance for viewers");
    }

    private static WorkOrderManagePageContext NewModePage()
    {
        var creator = WorkOrderManageSteps.Viewer();
        return WorkOrderManagePageContext.Create()
            .SignedInAs(creator)
            .WithNewOrder(o => o.WithId(Guid.NewGuid()).WithNumber("WO-INSTR").WithTitle("Instructions field"));
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
        completed.Instructions = "Saved guidance for viewers";
        return WorkOrderManagePageContext.Create().SignedInAs(viewer).Editing(completed);
    }
}
