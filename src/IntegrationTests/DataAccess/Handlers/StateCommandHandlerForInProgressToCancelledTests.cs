using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

public class StateCommandHandlerForInProgressToCancelledTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldSaveWorkOrderWithNoAssigneeAndCreator()
    {
        var currentUser = EmployeeBuilder.Create(TestData.UserName("ipc_cu")).Build();
        var assignee = EmployeeBuilder.Create(TestData.UserName("ipc_as")).Build();
        var order = WorkOrderBuilder.Create().WithId(Guid.Empty)
            .CreatedBy(currentUser).AssignedTo(assignee)
            .WithStatus(WorkOrderStatus.InProgress).Build();
        await StateCommandHandlerSteps.PersistAsync(currentUser, assignee);

        var result = await StateCommandHandlerSteps.HandleRemotedAsync(
            new InProgressToCancelledCommand(order, currentUser));

        ThenCancelledCleared(result.WorkOrder.Id, currentUser);
    }

    [Test]
    public async Task ShouldSaveWorkOrderWithOnlyCreatorRemotingCommand()
    {
        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        var currentUser = Faker<Employee>();
        o.Creator = currentUser;
        o.Status = WorkOrderStatus.InProgress;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(o);
            await context.SaveChangesAsync();
        }

        var command = new InProgressToCancelledCommand(o, currentUser);
        var remotedCommand = RemotableRequestTests.SimulateRemoteObject(command);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(remotedCommand);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe(order.Title);
        order.Description.ShouldBe(order.Description);
        order.Creator.ShouldBe(currentUser);
        order.Status.ShouldBe(WorkOrderStatus.Cancelled);
    }

    [Test]
    public async Task ShouldSaveWorkOrderWithOnlyCreatorRemotingWorkOrder()
    {
        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        var currentUser = Faker<Employee>();
        o.Creator = currentUser;
        o.Status = WorkOrderStatus.InProgress;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(o);
            await context.SaveChangesAsync();
        }

        var remotedOrder = RemotableRequestTests.SimulateRemoteObject(o);
        var command = new InProgressToCancelledCommand(remotedOrder, currentUser);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(command);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe(order.Title);
        order.Description.ShouldBe(order.Description);
        order.Creator.ShouldBe(currentUser);
        order.Status.ShouldBe(WorkOrderStatus.Cancelled);
    }

    [Test]
    public async Task ShouldNotBeValidWhenCurrentUserIsNotCreator()
    {
        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        var creator = Faker<Employee>();
        var assignee = Faker<Employee>();
        var otherUser = Faker<Employee>();
        o.Creator = creator;
        o.Assignee = assignee;
        o.Status = WorkOrderStatus.InProgress;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(otherUser);
            await context.SaveChangesAsync();
        }

        var command = new InProgressToCancelledCommand(o, otherUser);

        command.IsValid().ShouldBeFalse();
    }

    private static void ThenCancelledCleared(Guid id, Employee currentUser)
    {
        var order = StateCommandHandlerSteps.RequireOrder(id);
        order.Title.ShouldBe(order.Title);
        order.Description.ShouldBe(order.Description);
        order.Creator.ShouldBe(currentUser);
        order.Assignee.ShouldBeNull();
        order.AssignedDate.ShouldBeNull();
        order.Status.ShouldBe(WorkOrderStatus.Cancelled);
    }
}
