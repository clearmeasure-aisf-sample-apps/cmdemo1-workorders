using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

public class StateCommandHandlerForSaveTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldSaveWorkOrderBySavingDraft()
    {
        var currentUser = EmployeeBuilder.Create(TestData.UserName("save_draft"))
            .WithId(Guid.NewGuid()).Build();
        await StateCommandHandlerSteps.PersistAsync(currentUser);
        var workOrder = WorkOrderBuilder.Create().WithId(Guid.Empty)
            .CreatedBy(currentUser).CreatedAt(null).Build();
        workOrder.Instructions = "Turn off water main first";

        var result = await StateCommandHandlerSteps.HandleRemotedAsync(
            new SaveDraftCommand(workOrder, currentUser));

        ThenDraftSaved(result, workOrder, currentUser);
    }

    [Test]
    public async Task ShouldSaveAndReadBackMaxLengthTitle()
    {
        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var expectedTitle = new string('T', WorkOrder.TitleMaxLength);
        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.Creator = currentUser;
        workOrder.Title = expectedTitle;

        var result = await TestHost.GetRequiredService<IBus>().Send(new SaveDraftCommand(workOrder, currentUser));

        var readContext = TestHost.GetRequiredService<DbContext>();
        var order = readContext.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe(expectedTitle);
        order.Title!.Length.ShouldBe(WorkOrder.TitleMaxLength);
    }

    [Test]
    public async Task ShouldSaveAndReadBackMaxLengthBuilding()
    {
        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var expectedBuilding = new string('B', WorkOrder.BuildingMaxLength);
        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.Creator = currentUser;
        workOrder.Building = expectedBuilding;

        var result = await TestHost.GetRequiredService<IBus>().Send(new SaveDraftCommand(workOrder, currentUser));

        var readContext = TestHost.GetRequiredService<DbContext>();
        var order = readContext.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Building.ShouldBe(expectedBuilding);
        order.Building!.Length.ShouldBe(WorkOrder.BuildingMaxLength);
    }

    [Test]
    public async Task ShouldSaveWorkOrderWithAssigneeAndCreator()
    {
        var currentUser = EmployeeBuilder.Create(TestData.UserName("save_ac")).Build();
        var workOrder = WorkOrderBuilder.Create().CreatedBy(currentUser).Build();
        await StateCommandHandlerSteps.PersistAsync(currentUser, workOrder);
        await AssignSelfFromDbAsync(workOrder, currentUser.Id);

        await StateCommandHandlerSteps.HandleRemotedAsync(new SaveDraftCommand(workOrder, currentUser));

        ThenPartiesMatch(workOrder.Id, workOrder, currentUser);
    }

    [Test]
    public async Task ShouldUpdateWorkOrderWithAssigneeAndCreator()
    {
        var currentUser = EmployeeBuilder.Create(TestData.UserName("save_upd")).Build();
        var workOrder = WorkOrderBuilder.Create().CreatedBy(currentUser).Build();
        await StateCommandHandlerSteps.PersistAsync(currentUser, workOrder);
        await AssignSelfFromDbAsync(workOrder, currentUser.Id);
        ApplyUpdate(workOrder, currentUser);

        await StateCommandHandlerSteps.HandleRemotedAsync(new SaveDraftCommand(workOrder, currentUser));

        ThenUpdated(workOrder.Id, workOrder, currentUser);
    }

    [Test]
    public async Task ShouldUpdateWorkOrderWithAssigneeAndCreatorWithRemotedOrder()
    {
        var currentUser = EmployeeBuilder.Create(TestData.UserName("save_rem")).Build();
        var workOrder = WorkOrderBuilder.Create().CreatedBy(currentUser).Build();
        await StateCommandHandlerSteps.PersistAsync(currentUser, workOrder);
        workOrder.Creator = currentUser;
        await AssignSelfFromDbAsync(workOrder, currentUser.Id);
        workOrder.Title = "newtitle";

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workOrder, currentUser));
        _ = RemotableRequestTests.SimulateRemoteObject(command);
        await TestHost.GetRequiredService<StateCommandHandler>().Handle(command);

        ThenPartiesMatch(workOrder.Id, workOrder, currentUser);
    }

    [Test]
    public async Task ShouldRejectSecondInsert_WhenNumberAlreadyExists()
    {
        var duplicateNumber = TestData.Number("P");
        var currentUser = EmployeeBuilder.Create(TestData.UserName("save_dup"))
            .WithId(Guid.NewGuid()).Build();
        await StateCommandHandlerSteps.PersistAsync(currentUser);
        var first = DraftWithNumber(duplicateNumber, currentUser)[0];
        var second = DraftWithNumber(duplicateNumber, currentUser)[0];

        await StateCommandHandlerSteps.HandleRemotedAsync(new SaveDraftCommand(first, currentUser));
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            StateCommandHandlerSteps.HandleRemotedAsync(new SaveDraftCommand(second, currentUser)));

        await ThenOnlyOneWithNumberAsync(duplicateNumber, exception);
    }

    private static WorkOrder[] DraftWithNumber(string number, Employee creator) =>
        [WorkOrderBuilder.Create().WithId(Guid.Empty).WithNumber(number).CreatedBy(creator).Build()];

    private static async Task AssignSelfFromDbAsync(WorkOrder workOrder, Guid employeeId)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        workOrder.Assignee = context.Find<Employee>(employeeId);
    }

    private static void ApplyUpdate(WorkOrder workOrder, Employee currentUser)
    {
        workOrder.Creator = currentUser;
        workOrder.Title = "newtitle";
        workOrder.Instructions = "Updated guidance after inspection.";
    }

    private static void ThenDraftSaved(StateCommandResult result, WorkOrder workOrder, Employee currentUser)
    {
        result.TransitionVerbPresentTense.ShouldBe("Save");
        result.WorkOrder.Creator.ShouldBe(currentUser);
        result.WorkOrder.Title.ShouldBe(workOrder.Title);
        result.WorkOrder.CreatedDate.ShouldBe(TestHost.TestTime.DateTime);
        result.WorkOrder.Id.ShouldNotBe(Guid.Empty);
        var order = StateCommandHandlerSteps.RequireOrder(result.WorkOrder.Id);
        order.CreatedDate.ShouldBe(TestHost.TestTime.DateTime);
        order.Title.ShouldBe(workOrder.Title);
        order.Instructions.ShouldBe("Turn off water main first");
    }

    private static void ThenPartiesMatch(Guid id, WorkOrder expected, Employee currentUser)
    {
        var order = StateCommandHandlerSteps.RequireOrder(id);
        order.Title.ShouldBe(expected.Title);
        order.Description.ShouldBe(expected.Description);
        order.Creator.ShouldBe(currentUser);
        order.Assignee.ShouldBe(expected.Assignee);
    }

    private static void ThenUpdated(Guid id, WorkOrder expected, Employee currentUser)
    {
        ThenPartiesMatch(id, expected, currentUser);
        StateCommandHandlerSteps.RequireOrder(id).Instructions.ShouldBe("Updated guidance after inspection.");
    }

    private static async Task ThenOnlyOneWithNumberAsync(string number, InvalidOperationException exception)
    {
        exception.Message.ShouldContain(number);
        await using var verify = TestHost.GetRequiredService<DbContext>();
        (await verify.Set<WorkOrder>().CountAsync(w => w.Number == number)).ShouldBe(1);
    }
}
