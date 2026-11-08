using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

/// <summary>
/// The summary query matches <see cref="EmployeeWorkLoadQueryHandler"/> and reads the database once.
/// </summary>
[TestFixture]
public class EmployeeWorkLoadSummaryQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 18, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ShouldExecuteASingleSqlCommand_AndMatchPerEmployeeCounts()
    {
        var clock = TestData.FixedClock(Now);
        var today = ChurchTimeZone.Today(clock);
        var since = clock.GetUtcNow().DateTime.AddDays(-30);
        var creatorName = TestData.UserName("ewls_creator");
        var workerName = TestData.UserName("ewls_worker");
        var idleName = TestData.UserName("ewls_idle");

        var creator = new Employee(creatorName, "Cre", "Ator", TestData.Email(creatorName));
        creator.AddRole(new Role("Creator", true, false));
        var worker = new Employee(workerName, "Wor", "Ker", TestData.Email(workerName));
        worker.AddRole(new Role("Worker", false, true));
        worker.AddRole(new Role("Lead", true, true));
        var idle = new Employee(idleName, "Id", "Le", TestData.Email(idleName));
        idle.AddRole(new Role("Worker", false, true));

        await using var context = new QueryBudgetContext(TestHost.GetRequiredService<IDatabaseConfiguration>());
        context.AddRange(creator, worker, idle);
        context.Add(Order(creator, worker, WorkOrderStatus.Draft, today.AddDays(5)));
        context.Add(Order(creator, worker, WorkOrderStatus.Assigned, today.AddDays(-3)));
        context.Add(Order(creator, worker, WorkOrderStatus.InProgress, today.AddDays(-1)));
        context.Add(Order(creator, worker, WorkOrderStatus.InProgress, null));
        context.Add(Order(creator, worker, WorkOrderStatus.Complete, today.AddDays(-10), since.AddDays(1)));
        context.Add(Order(creator, worker, WorkOrderStatus.Complete, today.AddDays(-90), since.AddDays(-60)));
        context.Add(Order(creator, worker, WorkOrderStatus.Cancelled, today.AddDays(-2)));
        context.Add(Order(creator, null, WorkOrderStatus.Assigned, today.AddDays(-2)));
        context.Add(Order(creator, creator, WorkOrderStatus.Assigned, today.AddDays(-4)));
        await context.SaveChangesAsync();

        EmployeeWorkLoadSummary[] summaries;
        using (var budget = context.Begin())
        {
            summaries = await new EmployeeWorkLoadSummaryQueryHandler(context, clock)
                .Handle(new EmployeeWorkLoadSummaryQuery());

            budget.ReaderCount.ShouldBe(1, budget.Describe());
            budget.OtherCount.ShouldBe(0);
            var sql = budget.CommandTexts.Single();
            sql.ShouldContain("GROUP BY");
            sql.ShouldContain("SUM(");
        }

        summaries.ShouldNotContain(summary => summary.Employee.UserName == creatorName);
        var workerSummary = summaries.Single(summary => summary.Employee.UserName == workerName);
        workerSummary.Employee.Roles.Count.ShouldBe(2);
        workerSummary.Load.AssignedAndOpen.ShouldBe(4);
        workerSummary.Load.InProgress.ShouldBe(2);
        workerSummary.Load.Overdue.ShouldBe(2);
        workerSummary.Load.CompletedLast30Days.ShouldBe(1);

        var idleSummary = summaries.Single(summary => summary.Employee.UserName == idleName);
        idleSummary.Load.AssignedAndOpen.ShouldBe(0);
        idleSummary.Load.InProgress.ShouldBe(0);
        idleSummary.Load.Overdue.ShouldBe(0);
        idleSummary.Load.CompletedLast30Days.ShouldBe(0);

        var perEmployee = new EmployeeWorkLoadQueryHandler(context, clock);
        var workerLoad = await perEmployee.Handle(new EmployeeWorkLoadQuery(workerName));
        workerSummary.Load.AssignedAndOpen.ShouldBe(workerLoad.AssignedAndOpen);
        workerSummary.Load.InProgress.ShouldBe(workerLoad.InProgress);
        workerSummary.Load.Overdue.ShouldBe(workerLoad.Overdue);
        workerSummary.Load.CompletedLast30Days.ShouldBe(workerLoad.CompletedLast30Days);
        var idleLoad = await perEmployee.Handle(new EmployeeWorkLoadQuery(idleName));
        idleSummary.Load.AssignedAndOpen.ShouldBe(idleLoad.AssignedAndOpen);
        idleSummary.Load.CompletedLast30Days.ShouldBe(idleLoad.CompletedLast30Days);
    }

    private static WorkOrder Order(Employee creator, Employee? assignee, WorkOrderStatus status, DateOnly? dueDate,
        DateTime? completedDate = null)
    {
        return new WorkOrder
        {
            Number = TestData.Number("S"),
            Creator = creator,
            Assignee = assignee,
            Status = status,
            DueDate = dueDate,
            CompletedDate = completedDate
        };
    }
}
