using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class DueDateValidationTests
{
    [Test]
    public async Task ShouldShowMessageAndNotSave_WhenNewWorkOrderDueDateIsInThePast()
    {
        var user = WorkOrderManageSteps.Creator();
        var page = NewPage(user);
        await using var ctx = page.Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = page.Render(ctx);
        await FillRequiredFields(component);
        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DueDate))
            .ChangeAsync(new() { Value = "2020-01-01" });

        await component.Find(CommandButton("Save")).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DueDateValidation)).TextContent
                .ShouldContain(WorkOrder.DueDateInThePastMessage);
            page.Bus.LastCommand.ShouldBeNull();
            navigation.Uri.ShouldNotContain("/workorder/search");
        });
    }

    [Test]
    public async Task ShouldSave_WhenNewWorkOrderDueDateIsToday()
    {
        var user = WorkOrderManageSteps.Creator();
        var page = NewPage(user);
        await using var ctx = page.Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var today = ChurchTimeZone.Today(TimeProvider.System);
        var component = page.Render(ctx);
        await FillRequiredFields(component);
        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DueDate))
            .ChangeAsync(new() { Value = today.ToString("yyyy-MM-dd") });

        await component.Find(CommandButton("Save")).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            var command = page.Bus.LastCommand.ShouldBeOfType<SaveDraftCommand>();
            command.WorkOrder.DueDate.ShouldBe(today);
            command.PreviousDueDate.ShouldBeNull();
            navigation.Uri.ShouldContain("/workorder/search");
        });
    }

    [Test]
    public async Task ShouldSave_WhenNewWorkOrderDueDateIsEmpty()
    {
        var user = WorkOrderManageSteps.Creator();
        var page = NewPage(user);
        await using var ctx = page.Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = page.Render(ctx);
        await FillRequiredFields(component);

        await component.Find(CommandButton("Save")).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            var command = page.Bus.LastCommand.ShouldBeOfType<SaveDraftCommand>();
            command.WorkOrder.DueDate.ShouldBeNull();
            navigation.Uri.ShouldContain("/workorder/search");
        });
    }

    [Test]
    public async Task ShouldSave_WhenExistingPastDueDateIsUnchanged()
    {
        var user = WorkOrderManageSteps.Creator();
        var past = ChurchTimeZone.Today(TimeProvider.System).AddDays(-5);
        var existing = WorkOrderBuilder.Create()
            .WithId(Guid.NewGuid()).WithNumber("WO-PAST").WithTitle("Existing").WithDescription("Desc")
            .WithStatus(WorkOrderStatus.Draft).CreatedBy(user).DueOn(past).Build();
        var page = WorkOrderManagePageContext.Create()
            .SignedInAs(user).Editing(existing).WhenBusReceives<IStateCommand>();
        await using var ctx = page.Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = page.Render(ctx);
        await component.WaitForAssertionAsync(() =>
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Title))
                .GetAttribute("value").ShouldBe("Existing"));
        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Title))
            .ChangeAsync(new() { Value = "Edited title" });

        await component.Find(CommandButton("Save")).ClickAsync(new());

        await AssertPastDueSaved(component, page, navigation, past);
    }

    private static async Task AssertPastDueSaved(
        IRenderedComponent<WorkOrderManagePage> component,
        WorkOrderManagePageContext page,
        NavigationManager navigation,
        DateOnly past)
    {
        await component.WaitForAssertionAsync(() =>
        {
            var command = page.Bus.LastCommand.ShouldBeOfType<SaveDraftCommand>();
            command.PreviousDueDate.ShouldBe(past);
            command.WorkOrder.DueDate.ShouldBe(past);
            command.WorkOrder.Title.ShouldBe("Edited title");
            navigation.Uri.ShouldContain("/workorder/search");
        });
    }

    private static WorkOrderManagePageContext NewPage(Employee user) =>
        WorkOrderManagePageContext.Create()
            .SignedInAs(user)
            .WhenBusReceives<IStateCommand>()
            .WithNewOrder(o => o.WithTitle("").WithDescription("").WithNumber("WO-NEW"));

    private static async Task FillRequiredFields(IRenderedComponent<WorkOrderManagePage> component)
    {
        await component.WaitForAssertionAsync(() =>
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Title)).ShouldNotBeNull());
        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Title))
            .ChangeAsync(new() { Value = "Submit title" });
        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Description))
            .ChangeAsync(new() { Value = "Submit description" });
    }

    private static string CommandButton(string name) =>
        $"[data-testid='{WorkOrderManagePage.Elements.CommandButton}{name}']";
}
