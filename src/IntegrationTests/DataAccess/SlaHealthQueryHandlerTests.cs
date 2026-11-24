using System.Globalization;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.IntegrationTests.TestSupport;
using ClearMeasure.Bootcamp.UnitTests;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class SlaHealthQueryHandlerTests
{
    [Test]
    public async Task Should_ReturnNullPercentage_When_NoCompletedOrders()
    {
        var (clock, creator) = GivenSlaCreator();
        var order = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(TestData.Number()).DueOn(ChurchTimeZone.Today(clock)).Build();

        await using var context = TestHost.NewDbContext();
        await GivenPersistedAsync(context, creator, order);

        var result = await WhenSlaQueriedThrough(context, clock);

        result.CurrentMonthPercentage.ShouldBeNull();
    }

    [Test]
    public async Task Should_ExcludeOrders_Without_DueDate()
    {
        var (clock, creator) = GivenSlaCreator();
        var today = ChurchTimeZone.Today(clock);
        var completedOrder = WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(TestData.Number())
            .CompletedAt(today.ToDateTime(new TimeOnly(12, 0))).Build();

        await using var context = TestHost.NewDbContext();
        await GivenPersistedAsync(context, creator, completedOrder);

        var result = await WhenSlaQueriedThrough(context, clock);

        result.CurrentMonthPercentage.ShouldBeNull();
    }

    [Test]
    public async Task Should_CountOnTimeCompletion_When_CompletedDateEqualsOrBeforeDueDate()
    {
        var (clock, creator) = GivenSlaCreator();
        var dueDate = ChurchTimeZone.Today(clock);
        var onTimeOrder = WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(TestData.Number()).DueOn(dueDate)
            .CompletedAt(dueDate.ToDateTime(new TimeOnly(12, 0))).Build();

        await using var context = TestHost.NewDbContext();
        await GivenPersistedAsync(context, creator, onTimeOrder);

        var result = await WhenSlaQueriedThrough(context, clock);

        result.CurrentMonthPercentage.ShouldBe(100m);
        result.CurrentMonthOrderCount.ShouldBe(1);
    }

    [Test]
    public async Task Should_CountLateCompletion_As_NotOnTime()
    {
        var (clock, creator) = GivenSlaCreator();
        var today = ChurchTimeZone.Today(clock);
        var lateOrder = WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(TestData.Number()).DueOn(today.AddDays(-1))
            .CompletedAt(today.ToDateTime(new TimeOnly(12, 0))).Build();

        await using var context = TestHost.NewDbContext();
        await GivenPersistedAsync(context, creator, lateOrder);

        var result = await WhenSlaQueriedThrough(context, clock);

        result.CurrentMonthPercentage.ShouldBe(0m);
        result.CurrentMonthOrderCount.ShouldBe(1);
    }

    [Test]
    public async Task Should_CalculatePercentage_With_MixedOnTimeAndLate()
    {
        var (clock, creator) = GivenSlaCreator();
        var today = ChurchTimeZone.Today(clock);
        var noon = today.ToDateTime(new TimeOnly(12, 0));

        await using var context = TestHost.NewDbContext();
        await GivenPersistedAsync(context, creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .WithNumber(TestData.Number()).DueOn(today).CompletedAt(noon).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .WithNumber(TestData.Number()).DueOn(today.AddDays(-1)).CompletedAt(noon).Build());

        var result = await WhenSlaQueriedThrough(context, clock);

        result.CurrentMonthPercentage.ShouldBe(50m);
        result.CurrentMonthOrderCount.ShouldBe(2);
    }

    [Test]
    public async Task Should_SeparateMetrics_For_CurrentAndLastMonth()
    {
        var (clock, creator) = GivenSlaCreator();
        var today = ChurchTimeZone.Today(clock);
        var lastMonth = today.AddMonths(-1);

        await using var context = TestHost.NewDbContext();
        await GivenPersistedAsync(context, creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .WithNumber(TestData.Number()).DueOn(today)
                .CompletedAt(today.ToDateTime(new TimeOnly(12, 0))).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .WithNumber(TestData.Number()).DueOn(lastMonth)
                .CompletedAt(lastMonth.ToDateTime(new TimeOnly(12, 0))).Build());

        var result = await WhenSlaQueriedThrough(context, clock);

        result.CurrentMonthPercentage.ShouldBe(100m);
        result.CurrentMonthOrderCount.ShouldBe(1);
        result.LastMonthPercentage.ShouldBe(100m);
        result.LastMonthOrderCount.ShouldBe(1);
    }

    [Test]
    public async Task Should_FilterByCompletionMonth_NotDueMonth()
    {
        var (clock, creator) = GivenSlaCreator();
        var today = ChurchTimeZone.Today(clock);
        var order = WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(TestData.Number()).DueOn(today.AddMonths(1))
            .CompletedAt(today.ToDateTime(new TimeOnly(12, 0))).Build();

        await using var context = TestHost.NewDbContext();
        await GivenPersistedAsync(context, creator, order);

        var result = await WhenSlaQueriedThrough(context, clock);

        // Should be counted in current month (when it was completed), and it's on time
        result.CurrentMonthPercentage.ShouldBe(100m);
        result.CurrentMonthOrderCount.ShouldBe(1);
    }

    [Test]
    public async Task ShouldReadOnlyTheTwoChurchMonths_InOneQuery()
    {
        var (clock, creator) = GivenSlaCreator();
        var today = ChurchTimeZone.Today(clock);
        var window = GivenCompletionWindow(today);
        var orders = SlaHealthSteps.BuildWindowOrders(creator, today, window);

        await using var context = GivenCountedContext();
        await GivenPersistedAsync(context, creator, orders);
        context.Commands.Reset();

        var result = await WhenSlaQueriedThrough(context, clock);

        ThenWindowWasReadByOneCommand(context);
        ThenWindowSqlFiltersOnCompletedDates(context);
        ThenOnlyTheCompletedChurchMonthCounts(result);
    }

    /// <summary>Arranges a clock later than every stored date and the SLA tests' creator employee.</summary>
    private static (TimeProvider Clock, Employee Creator) GivenSlaCreator()
    {
        return (TestData.FutureClock(), ObjectMother.Employee(TestData.UserName("sla")));
    }

    private static async Task GivenPersistedAsync(DataContext context, Employee creator, params WorkOrder[] orders)
    {
        context.Add(creator);
        context.Set<WorkOrder>().AddRange(orders);
        await context.SaveChangesAsync();
    }

    private static Task<SlaHealth> WhenSlaQueriedThrough(DataContext context, TimeProvider clock)
    {
        var handler = new SlaHealthQueryHandler(context, clock);
        return handler.Handle(new SlaHealthQuery());
    }

    private static CountingDataContext GivenCountedContext()
    {
        return new CountingDataContext(TestHost.GetRequiredService<IDatabaseConfiguration>());
    }

    [TestCase("2026-01-15", "2025-12-01 06:00", "2026-02-01 06:00")]
    [TestCase("2026-03-15", "2026-02-01 06:00", "2026-04-01 05:00")]
    [TestCase("2026-11-09", "2026-10-01 05:00", "2026-12-01 06:00")]
    [TestCase("2028-02-29", "2028-01-01 06:00", "2028-03-01 06:00")]
    public void ShouldSpanLastMonthThroughCurrentMonth_WhenTheCompletionWindowIsBuilt(
        string today, string start, string end)
    {
        var window = GivenCompletionWindow(DateOnly.Parse(today, CultureInfo.InvariantCulture));

        window.Start.ShouldBe(DateTime.Parse(start, CultureInfo.InvariantCulture));
        window.EndExcluded.ShouldBe(DateTime.Parse(end, CultureInfo.InvariantCulture));
    }

    private static ChurchInstantRange GivenCompletionWindow(DateOnly today)
    {
        var currentMonth = ChurchMonth.Containing(today);
        var lastMonth = currentMonth.Previous();
        return currentMonth.CompletionWindow(lastMonth);
    }

    /// <summary>Asserts the two church months are answered by exactly one reader command.</summary>
    private static void ThenWindowWasReadByOneCommand(CountingDataContext context)
    {
        context.Commands.ReaderCommands.ShouldBe(1, string.Join(Environment.NewLine, context.Commands.Texts));
        context.Commands.OtherCommands.ShouldBe(0);
    }

    /// <summary>Asserts the single command filters on the completed date range and joins nothing.</summary>
    private static void ThenWindowSqlFiltersOnCompletedDates(CountingDataContext context)
    {
        var sql = context.Commands.Texts.Single();
        sql.ShouldContain("CompletedDate");
        sql.ShouldContain(">=");
        sql.ShouldContain("<");
        sql.ShouldNotContain("JOIN");
    }

    /// <summary>Asserts the completion counts in the last church month and nothing counts in the current one.</summary>
    private static void ThenOnlyTheCompletedChurchMonthCounts(SlaHealth result)
    {
        result.LastMonthPercentage.ShouldBe(100m);
        result.LastMonthOrderCount.ShouldBe(1);
        result.CurrentMonthPercentage.ShouldBeNull();
        result.CurrentMonthOrderCount.ShouldBe(0);
    }
}
