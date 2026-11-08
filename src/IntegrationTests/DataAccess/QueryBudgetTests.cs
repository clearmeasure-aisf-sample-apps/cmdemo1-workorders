using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

/// <summary>
/// Command budgets for the Teams and Vendors workload, SLA health, and reports queries.
/// The command count must stay flat as the seeded row count grows.
/// </summary>
[TestFixture]
public sealed class QueryBudgetTests
{
    private const int FewRows = 3;
    private const int ManyRows = 30;

    /// <summary>
    /// Teams and Vendors loads every assignable employee's workload in one grouped statement (#1491).
    /// </summary>
    private const int EmployeeWorkLoadSummaryCommandBudget = 1;

    /// <summary>
    /// Commands issued by <see cref="SlaHealthQueryHandler"/> today.
    /// The handler loads every completed order that has a due date in one read, then filters the
    /// months in memory, so the row set is unbounded. Issue #1522 will bound that read.
    /// This budget locks the current command count and does not change the handler.
    /// </summary>
    private const int SlaHealthCommandBudget = 1;

    /// <summary>
    /// One command each: overdue, open by status, open by priority, open by building,
    /// completions in the last eight weeks, and average days to close.
    /// </summary>
    private const int WorkOrderReportsCommandBudget = 6;

    private static readonly DateTimeOffset FixedNow = new(2026, 10, 14, 18, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ShouldCountCommands_OnlyWhileTheScopeIsOpen()
    {
        await using var context = NewContext();
        await context.Set<Employee>().AsNoTracking().CountAsync();

        var scope = context.Begin();
        try
        {
            await context.Set<Employee>().AsNoTracking().CountAsync();
            await context.Set<Role>().AsNoTracking().CountAsync();
            scope.CommandCount.ShouldBe(2, scope.Describe());
            scope.ReaderCount.ShouldBe(2, scope.Describe());
            scope.OtherCount.ShouldBe(0, scope.Describe());
            scope.CommandTexts.ShouldAllBe(text => text.Contains("SELECT", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            scope.Dispose();
        }

        await context.Set<Vendor>().AsNoTracking().CountAsync();
        scope.CommandCount.ShouldBe(2, scope.Describe());
    }

    [Test]
    public async Task ShouldRejectASecondScope_UntilTheFirstIsDisposed()
    {
        await using var context = NewContext();
        using (context.Begin())
        {
            var error = Should.Throw<InvalidOperationException>(() => context.Begin());
            error.Message.ShouldContain("already open");
        }
    }

    [Test]
    public async Task ShouldStartAFreshCount_ForEachScope()
    {
        await using var context = NewContext();
        using (var first = context.Begin())
        {
            await context.Set<Employee>().AsNoTracking().CountAsync();
            first.CommandCount.ShouldBe(1, first.Describe());
        }

        using var second = context.Begin();
        second.CommandCount.ShouldBe(0);
        await context.Set<Employee>().AsNoTracking().CountAsync();
        second.CommandCount.ShouldBe(1, second.Describe());
    }

    [Test]
    public async Task ShouldKeepEmployeeWorkLoadSummaryWithinBudget_WhenEmployeesGrowFrom3To30()
    {
        var clock = TestData.FixedClock(FixedNow);
        var small = await RunEmployeeWorkLoadSummary(FewRows, clock);
        var large = await RunEmployeeWorkLoadSummary(ManyRows, clock);
        ShouldMatchBudget(small, large, EmployeeWorkLoadSummaryCommandBudget);
    }

    [Test]
    public async Task ShouldKeepSlaHealthWithinBudget_WhenCompletedOrdersGrowFrom3To30()
    {
        var clock = TestData.FutureClock();
        var small = await RunSlaHealth(FewRows, clock);
        var large = await RunSlaHealth(ManyRows, clock);
        ShouldMatchBudget(small, large, SlaHealthCommandBudget);
    }

    [Test]
    public async Task ShouldKeepWorkOrderReportsWithinBudget_WhenOrdersGrowFrom3To30()
    {
        var clock = TestData.FixedClock(FixedNow);
        var small = await RunWorkOrderReports(FewRows, clock);
        var large = await RunWorkOrderReports(ManyRows, clock);
        ShouldMatchBudget(small, large, WorkOrderReportsCommandBudget);
    }

    private static async Task<BudgetResult> RunEmployeeWorkLoadSummary(int employeeCount, TimeProvider clock)
    {
        await using var context = NewContext();
        SeedAssignableEmployees(context, employeeCount, clock);
        await context.SaveChangesAsync();

        using var budget = context.Begin();
        var summaries = await new EmployeeWorkLoadSummaryQueryHandler(context, clock)
            .Handle(new EmployeeWorkLoadSummaryQuery());
        summaries.Length.ShouldBeGreaterThanOrEqualTo(employeeCount);
        return Budget(budget);
    }

    private static async Task<BudgetResult> RunSlaHealth(int orderCount, TimeProvider clock)
    {
        await using var context = NewContext();
        SeedCompletedOrders(context, orderCount, clock);
        await context.SaveChangesAsync();

        using var budget = context.Begin();
        var health = await new SlaHealthQueryHandler(context, clock).Handle(new SlaHealthQuery());
        health.CurrentMonthOrderCount.ShouldBeGreaterThanOrEqualTo(orderCount);
        return Budget(budget);
    }

    private static async Task<BudgetResult> RunWorkOrderReports(int orderCount, TimeProvider clock)
    {
        await using var context = NewContext();
        SeedOpenOrders(context, orderCount, clock);
        await context.SaveChangesAsync();

        using var budget = context.Begin();
        var data = await new WorkOrderReportsQueryHandler(context, clock).Handle(new WorkOrderReportsQuery());
        data.CompletedPerWeek.Count.ShouldBe(8);
        return Budget(budget);
    }

    private static void SeedAssignableEmployees(QueryBudgetContext context, int employeeCount, TimeProvider clock)
    {
        var today = ChurchTimeZone.Today(clock);
        var numbers = TestData.NumberSeries(employeeCount);
        var creator = NewEmployee("qbc");
        context.Add(creator);
        for (var index = 0; index < employeeCount; index++)
        {
            var worker = NewEmployee("qbw");
            worker.AddRole(new Role("Worker", canCreate: false, canFulfill: true));
            context.Add(worker);
            context.Add(new WorkOrder
            {
                Number = numbers[index],
                Title = "workload budget",
                Creator = creator,
                Assignee = worker,
                Status = WorkOrderStatus.Assigned,
                DueDate = today
            });
        }
    }

    private static void SeedCompletedOrders(QueryBudgetContext context, int orderCount, TimeProvider clock)
    {
        var today = ChurchTimeZone.Today(clock);
        var numbers = TestData.NumberSeries(orderCount);
        var creator = NewEmployee("qbs");
        context.Add(creator);
        var completed = today.ToDateTime(new TimeOnly(16, 0), DateTimeKind.Utc);
        for (var index = 0; index < orderCount; index++)
        {
            context.Add(new WorkOrder
            {
                Number = numbers[index],
                Title = "sla budget",
                Creator = creator,
                Status = WorkOrderStatus.Complete,
                DueDate = today,
                CompletedDate = completed
            });
        }
    }

    private static void SeedOpenOrders(QueryBudgetContext context, int orderCount, TimeProvider clock)
    {
        var numbers = TestData.NumberSeries(orderCount);
        var creator = NewEmployee("qbr");
        context.Add(creator);
        var created = clock.GetUtcNow().UtcDateTime;
        var today = ChurchTimeZone.Today(clock);
        for (var index = 0; index < orderCount; index++)
        {
            context.Add(new WorkOrder
            {
                Number = numbers[index],
                Title = "reports budget",
                Creator = creator,
                Status = WorkOrderStatus.Draft,
                CreatedDate = created,
                Building = "Hall",
                DueDate = today
            });
        }
    }

    private static Employee NewEmployee(string prefix)
    {
        var userName = TestData.UserName(prefix);
        return new Employee(userName, "Budget", "Tester", TestData.Email(userName));
    }

    private static QueryBudgetContext NewContext()
    {
        return new QueryBudgetContext(TestHost.GetRequiredService<IDatabaseConfiguration>());
    }

    private static BudgetResult Budget(QueryBudgetScope scope)
    {
        return new BudgetResult(scope.CommandCount, scope.OtherCount, scope.Describe());
    }

    private static void ShouldMatchBudget(BudgetResult small, BudgetResult large, int budget)
    {
        small.CommandCount.ShouldBe(large.CommandCount,
            $"command count grew with seeded rows ({FewRows} vs {ManyRows})." +
            $"{Environment.NewLine}small:{Environment.NewLine}{small.Commands}" +
            $"{Environment.NewLine}large:{Environment.NewLine}{large.Commands}");
        small.OtherCount.ShouldBe(0, small.Commands);
        large.OtherCount.ShouldBe(0, large.Commands);
        small.CommandCount.ShouldBe(budget, small.Commands);
    }

    private sealed record BudgetResult(int CommandCount, int OtherCount, string Commands);
}
