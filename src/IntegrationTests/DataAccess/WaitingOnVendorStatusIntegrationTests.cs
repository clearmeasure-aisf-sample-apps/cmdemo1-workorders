using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class WaitingOnVendorStatusIntegrationTests : IntegratedTestBase
{
    [Test]
    public async Task InProgressToWaitingOnVendorCommand_ShouldTransitionStatusAndPersist()
    {
        new DatabaseTests().Clean();

        var assignee = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-001",
            Title = "Test WTV Transition",
            Status = WorkOrderStatus.InProgress,
            Creator = assignee,
            Assignee = assignee
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(assignee);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var command = new InProgressToWaitingOnVendorCommand(workOrder, assignee);
        var result = await bus.Send(command);

        result.WorkOrder.Status.ShouldBe(WorkOrderStatus.WaitingOnVendor);

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var persisted = context.Set<WorkOrder>().SingleOrDefault(wo => wo.Number == "WO-001");
            persisted.ShouldNotBeNull();
            persisted.Status.ShouldBe(WorkOrderStatus.WaitingOnVendor);
        }
    }

    [Test]
    public async Task WaitingOnVendorToInProgressCommand_ShouldTransitionBackAndPersist()
    {
        new DatabaseTests().Clean();

        var assignee = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-002",
            Title = "Test Resume from WTV",
            Status = WorkOrderStatus.WaitingOnVendor,
            Creator = assignee,
            Assignee = assignee
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(assignee);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var command = new WaitingOnVendorToInProgressCommand(workOrder, assignee);
        var result = await bus.Send(command);

        result.WorkOrder.Status.ShouldBe(WorkOrderStatus.InProgress);

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var persisted = context.Set<WorkOrder>().SingleOrDefault(wo => wo.Number == "WO-002");
            persisted.ShouldNotBeNull();
            persisted.Status.ShouldBe(WorkOrderStatus.InProgress);
        }
    }

    [Test]
    public async Task WorkOrderSpecificationQuery_WithOpenOnly_ShouldIncludeWaitingOnVendorOrders()
    {
        new DatabaseTests().Clean();

        var assignee = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        var draftOrder = new WorkOrder { Number = "WO-DRAFT", Status = WorkOrderStatus.Draft, Creator = assignee };
        var assignedOrder = new WorkOrder { Number = "WO-ASSIGNED", Status = WorkOrderStatus.Assigned, Creator = assignee };
        var inProgressOrder = new WorkOrder { Number = "WO-IP", Status = WorkOrderStatus.InProgress, Creator = assignee };
        var wtvOrder = new WorkOrder { Number = "WO-WTV", Status = WorkOrderStatus.WaitingOnVendor, Creator = assignee };
        var completeOrder = new WorkOrder { Number = "WO-COMPLETE", Status = WorkOrderStatus.Complete, Creator = assignee };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(assignee);
            context.AddRange(draftOrder, assignedOrder, inProgressOrder, wtvOrder, completeOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var query = new WorkOrderSpecificationQuery();
        query.OpenOnly = true;
        var results = await bus.Send(query);

        var numbers = results.Select(wo => wo.Number).ToList();
        numbers.ShouldContain("WO-DRAFT");
        numbers.ShouldContain("WO-ASSIGNED");
        numbers.ShouldContain("WO-IP");
        numbers.ShouldContain("WO-WTV");
        numbers.ShouldNotContain("WO-COMPLETE");
        results.Length.ShouldBe(4);
    }

    [Test]
    public async Task WorkOrderSpecificationQuery_WithWaitingOnVendorStatus_ShouldReturnOnlyWtvOrders()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        var wtvOrder1 = new WorkOrder { Number = "WO-WTV-1", Status = WorkOrderStatus.WaitingOnVendor, Creator = creator };
        var wtvOrder2 = new WorkOrder { Number = "WO-WTV-2", Status = WorkOrderStatus.WaitingOnVendor, Creator = creator };
        var inProgressOrder = new WorkOrder { Number = "WO-IP", Status = WorkOrderStatus.InProgress, Creator = creator };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.AddRange(wtvOrder1, wtvOrder2, inProgressOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var query = new WorkOrderSpecificationQuery();
        query.MatchStatus(WorkOrderStatus.WaitingOnVendor);
        var results = await bus.Send(query);

        results.Length.ShouldBe(2);
        results.All(wo => wo.Status == WorkOrderStatus.WaitingOnVendor).ShouldBeTrue();
        results.Select(wo => wo.Number).ShouldContain("WO-WTV-1");
        results.Select(wo => wo.Number).ShouldContain("WO-WTV-2");
    }
}
