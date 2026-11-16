using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
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
        var names = new Names(
            TestData.UserName("ewls_creator"),
            TestData.UserName("ewls_worker"),
            TestData.UserName("ewls_idle"));

        await using var context = new QueryBudgetContext(TestHost.GetRequiredService<IDatabaseConfiguration>());
        await GivenWorkloadAsync(context, names, today, since);

        var summaries = await WhenSummaryQueried(context, clock);
        ThenWorkerAndIdleMatch(summaries, names, context, clock);
    }

    private static async Task GivenWorkloadAsync(
        QueryBudgetContext context, Names names, DateOnly today, DateTime since)
    {
        var creator = EmployeeBuilder.Create(names.Creator)
            .WithFirstName("Cre").WithLastName("Ator").WithEmail(TestData.Email(names.Creator))
            .WithRole("Creator", canCreate: true, canFulfill: false).Build();
        var worker = EmployeeBuilder.Create(names.Worker)
            .WithFirstName("Wor").WithLastName("Ker").WithEmail(TestData.Email(names.Worker))
            .WithRole("Worker", canCreate: false, canFulfill: true)
            .WithRole("Lead", canCreate: true, canFulfill: true).Build();
        var idle = EmployeeBuilder.Create(names.Idle)
            .WithFirstName("Id").WithLastName("Le").WithEmail(TestData.Email(names.Idle))
            .WithRole("Worker", canCreate: false, canFulfill: true).Build();

        context.AddRange(creator, worker, idle);
        context.AddRange(
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).AssignedTo(worker).DueOn(today.AddDays(5)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).AssignedTo(worker).DueOn(today.AddDays(-3)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).AssignedTo(worker).DueOn(today.AddDays(-1)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).AssignedTo(worker).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).AssignedTo(worker)
                .DueOn(today.AddDays(-10)).CompletedAt(since.AddDays(1)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).AssignedTo(worker)
                .DueOn(today.AddDays(-90)).CompletedAt(since.AddDays(-60)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Cancelled).AssignedTo(worker).DueOn(today.AddDays(-2)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).DueOn(today.AddDays(-2)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).AssignedTo(creator).DueOn(today.AddDays(-4)).Build());
        await context.SaveChangesAsync();
    }

    private static async Task<EmployeeWorkLoadSummary[]> WhenSummaryQueried(
        QueryBudgetContext context, TimeProvider clock)
    {
        using var budget = context.Begin();
        var summaries = await new EmployeeWorkLoadSummaryQueryHandler(context, clock)
            .Handle(new EmployeeWorkLoadSummaryQuery());

        budget.ReaderCount.ShouldBe(1, budget.Describe());
        budget.OtherCount.ShouldBe(0);
        var sql = budget.CommandTexts.Single();
        sql.ShouldContain("GROUP BY");
        sql.ShouldContain("SUM(");
        return summaries;
    }

    private static void ThenWorkerAndIdleMatch(
        EmployeeWorkLoadSummary[] summaries, Names names, QueryBudgetContext context, TimeProvider clock)
    {
        summaries.ShouldNotContain(summary => summary.Employee.UserName == names.Creator);
        var workerSummary = summaries.Single(summary => summary.Employee.UserName == names.Worker);
        workerSummary.Employee.Roles.Count.ShouldBe(2);
        ThenLoadIs(workerSummary.Load, Expected(4, 2, 2, 1));

        var idleSummary = summaries.Single(summary => summary.Employee.UserName == names.Idle);
        ThenLoadIs(idleSummary.Load, Expected(0, 0, 0, 0));
        ThenMatchesPerEmployee(new MatchContext(context, clock, names), workerSummary, idleSummary);
    }

    private static EmployeeWorkLoad Expected(
        int assignedAndOpen, int inProgress, int overdue, int completedLast30Days) =>
        new()
        {
            AssignedAndOpen = assignedAndOpen,
            InProgress = inProgress,
            Overdue = overdue,
            CompletedLast30Days = completedLast30Days
        };

    private static void ThenLoadIs(EmployeeWorkLoad load, EmployeeWorkLoad expected)
    {
        load.AssignedAndOpen.ShouldBe(expected.AssignedAndOpen);
        load.InProgress.ShouldBe(expected.InProgress);
        load.Overdue.ShouldBe(expected.Overdue);
        load.CompletedLast30Days.ShouldBe(expected.CompletedLast30Days);
    }

    private static void ThenMatchesPerEmployee(
        MatchContext match, EmployeeWorkLoadSummary workerSummary, EmployeeWorkLoadSummary idleSummary)
    {
        var perEmployee = new EmployeeWorkLoadQueryHandler(match.Context, match.Clock);
        var workerLoad = perEmployee.Handle(new EmployeeWorkLoadQuery(match.Names.Worker)).GetAwaiter().GetResult();
        ThenLoadIs(workerSummary.Load, workerLoad);
        var idleLoad = perEmployee.Handle(new EmployeeWorkLoadQuery(match.Names.Idle)).GetAwaiter().GetResult();
        idleSummary.Load.AssignedAndOpen.ShouldBe(idleLoad.AssignedAndOpen);
        idleSummary.Load.CompletedLast30Days.ShouldBe(idleLoad.CompletedLast30Days);
    }

    private sealed record Names(string Creator, string Worker, string Idle);

    private sealed record MatchContext(QueryBudgetContext Context, TimeProvider Clock, Names Names);
}
