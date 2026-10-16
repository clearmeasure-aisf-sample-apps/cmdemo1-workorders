using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class EmployeeWorkLoadQueryHandlerTests
{
    [Test]
    public async Task ShouldCountWorkLoadForEmployeeFromDatabase()
    {
        var ewlCreatorUserName = TestData.UserName("ewl_creator");
        var ewlWorkerUserName = TestData.UserName("ewl_worker");
        var ewlIdleUserName = TestData.UserName("ewl_idle");

        var creator = new Employee(ewlCreatorUserName, "Cre", "Ator", "creator@test.com");
        var worker = new Employee(ewlWorkerUserName, "Wor", "Ker", "worker@test.com");
        var idle = new Employee(ewlIdleUserName, "Id", "Le", "idle@test.com");
        var today = ChurchTimeZone.Today(TimeProvider.System);
        var recent = DateTime.UtcNow.AddDays(-5);
        var old = DateTime.UtcNow.AddDays(-60);

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(worker);
            context.Add(idle);
            context.Add(NewOrder(TestData.Number("E"), creator, worker, WorkOrderStatus.Draft, today.AddDays(5)));
            context.Add(NewOrder(TestData.Number("E"), creator, worker, WorkOrderStatus.Assigned, today.AddDays(-3)));
            context.Add(NewOrder(TestData.Number("E"), creator, worker, WorkOrderStatus.InProgress, today.AddDays(-1)));
            context.Add(NewOrder(TestData.Number("E"), creator, worker, WorkOrderStatus.InProgress, null));
            context.Add(NewOrder(TestData.Number("E"), creator, worker, WorkOrderStatus.Complete, today.AddDays(-10), recent));
            context.Add(NewOrder(TestData.Number("E"), creator, worker, WorkOrderStatus.Complete, today.AddDays(-90), old));
            context.Add(NewOrder(TestData.Number("E"), creator, worker, WorkOrderStatus.Cancelled, today.AddDays(-2)));
            context.Add(NewOrder(TestData.Number("E"), creator, null, WorkOrderStatus.Assigned, today.AddDays(-2)));
            await context.SaveChangesAsync();
        }

        var handler = new EmployeeWorkLoadQueryHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);

        var load = await handler.Handle(new EmployeeWorkLoadQuery(ewlWorkerUserName));
        load.AssignedAndOpen.ShouldBe(4);
        load.InProgress.ShouldBe(2);
        load.Overdue.ShouldBe(2);
        load.CompletedLast30Days.ShouldBe(1);

        var idleLoad = await handler.Handle(new EmployeeWorkLoadQuery(ewlIdleUserName));
        idleLoad.AssignedAndOpen.ShouldBe(0);
        idleLoad.InProgress.ShouldBe(0);
        idleLoad.Overdue.ShouldBe(0);
        idleLoad.CompletedLast30Days.ShouldBe(0);
    }

    private static WorkOrder NewOrder(string number, Employee creator, Employee? assignee,
        WorkOrderStatus status, DateOnly? dueDate, DateTime? completedDate = null)
    {
        return new WorkOrder
        {
            Number = number,
            Creator = creator,
            Assignee = assignee,
            Status = status,
            DueDate = dueDate,
            CompletedDate = completedDate
        };
    }
}
