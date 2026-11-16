using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

public class StateCommandHandlerForCancelTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldSaveWorkOrderWithNoAssigneeAndCreator()
    {
        var currentUser = EmployeeBuilder.Create(TestData.UserName("cancel_cu")).Build();
        var assignee = EmployeeBuilder.Create(TestData.UserName("cancel_as")).Build();
        var order = WorkOrderBuilder.Create().WithId(Guid.Empty)
            .CreatedBy(currentUser).Assigned(assignee).Build();
        await StateCommandHandlerSteps.PersistAsync(currentUser, assignee);

        var result = await StateCommandHandlerSteps.HandleRemotedAsync(
            new AssignedToCancelledCommand(order, currentUser));

        ThenCancelledCleared(result.WorkOrder.Id, currentUser);
    }

    [Test]
    public async Task ShouldSaveWorkOrderWaitingOnVendorWithNoAssigneeAndCreator()
    {
        var currentUser = EmployeeBuilder.Create(TestData.UserName("cancel_wov_cu")).Build();
        var assignee = EmployeeBuilder.Create(TestData.UserName("cancel_wov_as")).Build();
        var order = WorkOrderBuilder.Create().WithId(Guid.Empty)
            .CreatedBy(currentUser).AssignedTo(assignee)
            .WithStatus(WorkOrderStatus.InProgress)
            .WaitingOnVendorSince(DateTime.UtcNow).Build();
        await StateCommandHandlerSteps.PersistAsync(currentUser, assignee);

        var result = await StateCommandHandlerSteps.HandleRemotedAsync(
            new WaitingOnVendorToCancelledCommand(order, currentUser));

        ThenCancelledClearedFromWaiting(result.WorkOrder.Id, currentUser);
    }

    [Test]
    public async Task ShouldSaveWorkOrderWithOnlyCreatorRemotingCommand()
    {
        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        var currentUser = Faker<Employee>();
        o.Creator = currentUser;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(o);
            await context.SaveChangesAsync();
        }

        var command = new AssignedToCancelledCommand(o, currentUser);
        var remotedCommand = RemotableRequestTests.SimulateRemoteObject(command);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(remotedCommand);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe(order.Title);
        order.Description.ShouldBe(order.Description);
        order.Creator.ShouldBe(currentUser);
    }

    [Test]
    public async Task ShouldSaveWorkOrderWithOnlyCreatorRemotingWorkOrder()
    {
        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        var currentUser = Faker<Employee>();
        o.Creator = currentUser;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(o);
            await context.SaveChangesAsync();
        }

        var remotedOrder = RemotableRequestTests.SimulateRemoteObject(o);
        var command = new AssignedToCancelledCommand(remotedOrder, currentUser);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(command);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe(order.Title);
        order.Description.ShouldBe(order.Description);
        order.Creator.ShouldBe(currentUser);
    }

    [Test]
    public async Task ShouldSaveWorkOrderWaitingOnVendorWithOnlyCreatorRemotingCommand()
    {
        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        var currentUser = Faker<Employee>();
        o.Creator = currentUser;
        o.Status = WorkOrderStatus.InProgress;
        o.WaitingOnVendorSince = DateTime.UtcNow;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(o);
            await context.SaveChangesAsync();
        }

        var command = new WaitingOnVendorToCancelledCommand(o, currentUser);
        var remotedCommand = RemotableRequestTests.SimulateRemoteObject(command);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(remotedCommand);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe(order.Title);
        order.Description.ShouldBe(order.Description);
        order.Creator.ShouldBe(currentUser);
        order.WaitingOnVendorSince.ShouldBeNull();
    }

    [Test]
    public async Task ShouldSaveWorkOrderWaitingOnVendorWithOnlyCreatorRemotingWorkOrder()
    {
        var o = Faker<WorkOrder>();
        o.Id = Guid.Empty;
        var currentUser = Faker<Employee>();
        o.Creator = currentUser;
        o.Status = WorkOrderStatus.InProgress;
        o.WaitingOnVendorSince = DateTime.UtcNow;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(o);
            await context.SaveChangesAsync();
        }

        var remotedOrder = RemotableRequestTests.SimulateRemoteObject(o);
        var command = new WaitingOnVendorToCancelledCommand(remotedOrder, currentUser);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(command);

        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe(order.Title);
        order.Description.ShouldBe(order.Description);
        order.Creator.ShouldBe(currentUser);
        order.WaitingOnVendorSince.ShouldBeNull();
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

    private static void ThenCancelledClearedFromWaiting(Guid id, Employee currentUser)
    {
        ThenCancelledCleared(id, currentUser);
        StateCommandHandlerSteps.RequireOrder(id).WaitingOnVendorSince.ShouldBeNull();
    }
}
