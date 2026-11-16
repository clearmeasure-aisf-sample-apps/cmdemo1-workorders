using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class SubmitTests
{
    [Test]
    public async Task ShouldNavigateToSearch_WhenSaveCommandSubmittedForNewWorkOrder()
    {
        var user = WorkOrderManageSteps.Creator();
        var page = NewSubmitPage(user);
        await using var ctx = page.Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = page.Render(ctx);
        await WaitForTitle(component);
        await SetField(component, WorkOrderManagePage.Elements.Title, "Submit title");
        await SetField(component, WorkOrderManagePage.Elements.Description, "Submit description");

        await component.Find(CommandButton("Save")).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            page.Bus.LastCommand.ShouldNotBeNull();
            page.Bus.LastCommand.ShouldBeOfType<SaveDraftCommand>().PreviousRoomNumber.ShouldBeNull();
            navigation.Uri.ShouldContain("/workorder/search");
        });
    }

    [Test]
    public async Task Submit_ShouldReuseLoadedEmployee_WithoutSecondEmployeeLookup()
    {
        var user = WorkOrderManageSteps.Creator();
        var session = new StubUserSession(user);
        var page = NewSubmitPage(user, session);
        await using var ctx = page.Build();
        var component = page.Render(ctx);
        await WaitForTitle(component);
        await SetField(component, WorkOrderManagePage.Elements.Title, "Title");
        await SetField(component, WorkOrderManagePage.Elements.Description, "Desc");
        var callsAfterLoad = page.Session.GetCurrentUserCallCount;

        await component.Find(CommandButton("Save")).ClickAsync(new());

        await component.WaitForAssertionAsync(() => page.Bus.LastCommand.ShouldNotBeNull());
        page.Session.GetCurrentUserCallCount.ShouldBe(callsAfterLoad);
        page.Bus.EmployeeByUserNameHits.ShouldBe(0);
    }

    [Test]
    public async Task ShouldLoadExistingWorkOrder_WhenEditModeSubmitted()
    {
        var user = WorkOrderManageSteps.Creator();
        var existing = WorkOrderBuilder.Create()
            .WithId(Guid.NewGuid()).WithNumber("WO-EDIT").WithTitle("Existing").WithDescription("Desc")
            .WithStatus(WorkOrderStatus.Draft).CreatedBy(user).Build();
        var page = EditSubmitPage(user, existing);
        await using var ctx = page.Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = page.Render(ctx);
        await component.WaitForAssertionAsync(() =>
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Title))
                .GetAttribute("value").ShouldBe("Existing"));

        await component.Find(CommandButton("Save")).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            page.Bus.WorkOrderByNumberHits.ShouldBeGreaterThan(0);
            page.Bus.LastCommand.ShouldNotBeNull();
            navigation.Uri.ShouldContain("/workorder/search");
        });
    }

    [Test]
    public async Task ShouldSendPersistedRoomNumberAsPrevious_WhenEditModeRoomChanged()
    {
        var user = WorkOrderManageSteps.Creator();
        var existing = WorkOrderBuilder.Create()
            .WithId(Guid.NewGuid()).WithNumber("WO-ROOM").WithTitle("Existing").WithDescription("Desc")
            .InRoom("Room A").WithStatus(WorkOrderStatus.Draft).CreatedBy(user).Build();
        var page = EditSubmitPage(user, existing);
        await using var ctx = page.Build();
        var component = page.Render(ctx);
        await component.WaitForAssertionAsync(() =>
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Title))
                .GetAttribute("value").ShouldBe("Existing"));
        await SetField(component, WorkOrderManagePage.Elements.RoomNumber, "Room B");

        await component.Find(CommandButton("Save")).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            var command = page.Bus.LastCommand.ShouldBeOfType<SaveDraftCommand>();
            command.PreviousRoomNumber.ShouldBe("Room A");
            command.WorkOrder.RoomNumber.ShouldBe("Room B");
        });
    }

    [Test]
    public async Task HandleSubmit_SubmittedTwiceWhileInFlight_SendsOneCommand()
    {
        var user = WorkOrderManageSteps.Creator();
        var gate = new TaskCompletionSource();
        var page = NewSubmitPage(user, gate: gate.Task);
        await using var ctx = page.Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = page.Render(ctx);
        await WaitForTitle(component);
        await SetField(component, WorkOrderManagePage.Elements.Title, "Once");
        await SetField(component, WorkOrderManagePage.Elements.Description, "Only once");

        await AssertDoubleSubmitOnce(component, page, navigation, gate);
    }

    [Test]
    public async Task ShouldSendPriorityAndBuilding_WhenSaved()
    {
        var user = WorkOrderManageSteps.Creator();
        var page = NewSubmitPage(user);
        await using var ctx = page.Build();
        var component = page.Render(ctx);
        await component.WaitForAssertionAsync(() =>
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Priority)).ShouldNotBeNull());
        component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Priority)).GetAttribute("value")
            .ShouldBe(nameof(WorkOrderPriority.Medium));
        await SetField(component, WorkOrderManagePage.Elements.Title, "Organ");
        await SetField(component, WorkOrderManagePage.Elements.Description, "Tune it");
        await SetField(component, WorkOrderManagePage.Elements.Priority, nameof(WorkOrderPriority.High));
        await SetField(component, WorkOrderManagePage.Elements.Building, "  Sanctuary ");
        await SetField(component, WorkOrderManagePage.Elements.Floor, "  2 ");

        await component.Find(CommandButton("Save")).ClickAsync(new());

        await AssertPriorityBuildingSaved(component, page);
    }

    [Test]
    public async Task ShouldStayOnPage_WhenShelveReasonIsEmpty()
    {
        var user = WorkOrderManageSteps.Fulfiller();
        var page = EditSubmitPage(user, WorkOrderManageSteps.InProgressOwnedBy(user));
        await using var ctx = page.Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = page.Render(ctx);
        await component.WaitForAssertionAsync(() =>
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.ShelveReason)).ShouldNotBeNull());

        await component.Find(CommandButton("Shelve")).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            page.Bus.LastCommand.ShouldBeNull();
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.ShelveReasonError)).TextContent
                .ShouldContain("A reason is required");
            navigation.Uri.ShouldNotContain("/workorder/search");
        });
    }

    [Test]
    public async Task ShouldStayOnPage_WhenShelveReasonIsTooLong()
    {
        var user = WorkOrderManageSteps.Fulfiller();
        var page = EditSubmitPage(user, WorkOrderManageSteps.InProgressOwnedBy(user));
        await using var ctx = page.Build();
        var component = page.Render(ctx);
        await component.WaitForAssertionAsync(() =>
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.ShelveReason)).ShouldNotBeNull());
        await SetField(component, WorkOrderManagePage.Elements.ShelveReason,
            new string('r', InProgressToAssignedCommand.ReasonMaxLength + 1));

        await component.Find(CommandButton("Shelve")).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            page.Bus.LastCommand.ShouldBeNull();
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.ShelveReasonError)).TextContent
                .ShouldContain("cannot exceed");
        });
    }

    [Test]
    public async Task ShouldSendTrimmedReason_WhenShelveReasonIsValid()
    {
        var user = WorkOrderManageSteps.Fulfiller();
        var page = EditSubmitPage(user, WorkOrderManageSteps.InProgressOwnedBy(user));
        await using var ctx = page.Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = page.Render(ctx);
        await component.WaitForAssertionAsync(() =>
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.ShelveReason)).ShouldNotBeNull());
        await SetField(component, WorkOrderManagePage.Elements.ShelveReason, "  Waiting on parts  ");

        await component.Find(CommandButton("Shelve")).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            var command = page.Bus.LastCommand.ShouldBeOfType<InProgressToAssignedCommand>();
            command.Reason.ShouldBe("Waiting on parts");
            navigation.Uri.ShouldContain("/workorder/search");
        });
    }

    private static async Task AssertDoubleSubmitOnce(
        IRenderedComponent<WorkOrderManagePage> component,
        WorkOrderManagePageContext page,
        NavigationManager navigation,
        TaskCompletionSource gate)
    {
        var saveSelector = CommandButton("Save");
        _ = component.Find(saveSelector).ClickAsync(new());
        await component.WaitForAssertionAsync(() => page.Bus.CommandCount.ShouldBe(1));
        component.Find(saveSelector).HasAttribute("disabled").ShouldBeTrue();
        await component.Find("form").SubmitAsync();
        gate.SetResult();
        await component.WaitForAssertionAsync(() => navigation.Uri.ShouldContain("/workorder/search"));
        page.Bus.CommandCount.ShouldBe(1);
    }

    private static async Task AssertPriorityBuildingSaved(
        IRenderedComponent<WorkOrderManagePage> component,
        WorkOrderManagePageContext page)
    {
        await component.WaitForAssertionAsync(() =>
        {
            var command = page.Bus.LastCommand.ShouldBeOfType<SaveDraftCommand>();
            command.WorkOrder.Priority.ShouldBe(WorkOrderPriority.High);
            command.WorkOrder.Building.ShouldBe("Sanctuary");
            command.WorkOrder.Floor.ShouldBe("2");
        });
    }

    private static WorkOrderManagePageContext NewSubmitPage(
        Employee user, StubUserSession? session = null, Task? gate = null) =>
        WorkOrderManagePageContext.Create()
            .SignedInAs(user, session)
            .WhenBusReceives<IStateCommand>(gate)
            .WithNewOrder(o => o.WithTitle("").WithDescription("").WithNumber("WO-NEW"));

    private static WorkOrderManagePageContext EditSubmitPage(Employee user, WorkOrder existing) =>
        WorkOrderManagePageContext.Create()
            .SignedInAs(user)
            .Editing(existing)
            .WhenBusReceives<IStateCommand>();

    private static async Task WaitForTitle(IRenderedComponent<WorkOrderManagePage> component) =>
        await component.WaitForAssertionAsync(() =>
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Title)).ShouldNotBeNull());

    private static async Task SetField(
        IRenderedComponent<WorkOrderManagePage> component,
        WorkOrderManagePage.Elements element,
        string value) =>
        await component.Find(WorkOrderManageSteps.TestId(element)).ChangeAsync(new() { Value = value });

    private static string CommandButton(string name) =>
        $"[data-testid='{WorkOrderManagePage.Elements.CommandButton}{name}']";
}
