using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

public class StateCommandHandlerForShelveTests : IntegratedTestBase
{
    [Test]
    public async Task Should_RecordTrimmedReason_When_Shelved()
    {
        var order = Faker<WorkOrder>();
        order.Id = Guid.Empty;
        var currentUser = Faker<Employee>();
        order.Creator = currentUser;
        order.Assignee = currentUser;
        order.Status = WorkOrderStatus.InProgress;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        var command = new InProgressToAssignedCommand(order, currentUser) { Reason = "  Waiting on parts  " };
        var remotedCommand = RemotableRequestTests.SimulateRemoteObject(command);
        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(remotedCommand);

        await using var saved = TestHost.GetRequiredService<DbContext>();
        var persisted = saved.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        persisted.Status.ShouldBe(WorkOrderStatus.Assigned);
        var change = saved.Set<WorkOrderStatusChange>().Single(row => row.WorkOrderId == persisted.Id);
        change.FromStatus.ShouldBe(WorkOrderStatus.InProgress);
        change.ToStatus.ShouldBe(WorkOrderStatus.Assigned);
        change.Reason.ShouldBe("Waiting on parts");
    }

    [Test]
    public async Task Should_LeaveReasonNull_When_Completed()
    {
        var order = Faker<WorkOrder>();
        order.Id = Guid.Empty;
        var currentUser = Faker<Employee>();
        order.Creator = currentUser;
        order.Assignee = currentUser;
        order.Status = WorkOrderStatus.InProgress;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        var command = new InProgressToCompleteCommand(order, currentUser);
        var remotedCommand = RemotableRequestTests.SimulateRemoteObject(command);
        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(remotedCommand);

        await using var saved = TestHost.GetRequiredService<DbContext>();
        var change = saved.Set<WorkOrderStatusChange>().Single(row => row.WorkOrderId == result.WorkOrder.Id);
        change.ToStatus.ShouldBe(WorkOrderStatus.Complete);
        change.Reason.ShouldBeNull();
    }
}
