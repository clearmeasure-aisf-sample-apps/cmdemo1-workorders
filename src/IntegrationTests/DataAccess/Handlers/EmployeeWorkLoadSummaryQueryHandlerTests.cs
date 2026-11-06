using System.Data.Common;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

        await using var context = new CountingContext(TestHost.GetRequiredService<IDatabaseConfiguration>());
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

        context.Commands.Reset();
        var summaries = await new EmployeeWorkLoadSummaryQueryHandler(context, clock)
            .Handle(new EmployeeWorkLoadSummaryQuery());

        context.Commands.ReaderCommands.ShouldBe(1, string.Join(Environment.NewLine, context.Commands.Texts));
        context.Commands.OtherCommands.ShouldBe(0);
        var sql = context.Commands.Texts.Single();
        sql.ShouldContain("GROUP BY", Case.Insensitive);
        sql.ShouldContain("SUM(", Case.Insensitive);

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

    private sealed class CountingContext(IDatabaseConfiguration configuration) : DataContext(configuration)
    {
        public CommandCounter Commands { get; } = new();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(Commands);
            base.OnConfiguring(optionsBuilder);
        }
    }

    private sealed class CommandCounter : DbCommandInterceptor
    {
        public int ReaderCommands { get; private set; }
        public int OtherCommands { get; private set; }
        public List<string> Texts { get; } = [];

        public void Reset()
        {
            ReaderCommands = 0;
            OtherCommands = 0;
            Texts.Clear();
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ReaderCommands++;
            Texts.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            OtherCommands++;
            Texts.Add(command.CommandText);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            OtherCommands++;
            Texts.Add(command.CommandText);
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
