using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.IntegrationTests.TestSupport;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class SlaHealthQueryHandlerTests
{
    [Test]
    public async Task Should_ReturnNullPercentage_When_NoCompletedOrders()
    {
        var clock = TestData.FutureClock();
        var userName = TestData.UserName("sla");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Test").WithLastName("User").WithEmail(TestData.Email(userName)).Build();
        var order = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(TestData.Number()).DueOn(ChurchTimeZone.Today(clock)).Build();

        await GivenPersistedAsync(creator, order);

        var result = await WhenSlaQueried(clock);

        result.CurrentMonthPercentage.ShouldBeNull();
    }

    [Test]
    public async Task Should_ExcludeOrders_Without_DueDate()
    {
        var clock = TestData.FutureClock();
        var userName = TestData.UserName("sla");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Test").WithLastName("User").WithEmail(TestData.Email(userName)).Build();
        var today = ChurchTimeZone.Today(clock);
        var completedOrder = WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(TestData.Number())
            .CompletedAt(today.ToDateTime(new TimeOnly(12, 0))).Build();

        await GivenPersistedAsync(creator, completedOrder);

        var result = await WhenSlaQueried(clock);

        result.CurrentMonthPercentage.ShouldBeNull();
    }

    [Test]
    public async Task Should_CountOnTimeCompletion_When_CompletedDateEqualsOrBeforeDueDate()
    {
        var clock = TestData.FutureClock();
        var userName = TestData.UserName("sla");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Test").WithLastName("User").WithEmail(TestData.Email(userName)).Build();
        var dueDate = ChurchTimeZone.Today(clock);
        var onTimeOrder = WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(TestData.Number()).DueOn(dueDate)
            .CompletedAt(dueDate.ToDateTime(new TimeOnly(12, 0))).Build();

        await GivenPersistedAsync(creator, onTimeOrder);

        var result = await WhenSlaQueried(clock);

        result.CurrentMonthPercentage.ShouldBe(100m);
        result.CurrentMonthOrderCount.ShouldBe(1);
    }

    [Test]
    public async Task Should_CountLateCompletion_As_NotOnTime()
    {
        var clock = TestData.FutureClock();
        var userName = TestData.UserName("sla");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Test").WithLastName("User").WithEmail(TestData.Email(userName)).Build();
        var today = ChurchTimeZone.Today(clock);
        var lateOrder = WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(TestData.Number()).DueOn(today.AddDays(-1))
            .CompletedAt(today.ToDateTime(new TimeOnly(12, 0))).Build();

        await GivenPersistedAsync(creator, lateOrder);

        var result = await WhenSlaQueried(clock);

        result.CurrentMonthPercentage.ShouldBe(0m);
        result.CurrentMonthOrderCount.ShouldBe(1);
    }

    [Test]
    public async Task Should_CalculatePercentage_With_MixedOnTimeAndLate()
    {
        var clock = TestData.FutureClock();
        var userName = TestData.UserName("sla");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Test").WithLastName("User").WithEmail(TestData.Email(userName)).Build();
        var today = ChurchTimeZone.Today(clock);
        var noon = today.ToDateTime(new TimeOnly(12, 0));

        await GivenPersistedAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .WithNumber(TestData.Number()).DueOn(today).CompletedAt(noon).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .WithNumber(TestData.Number()).DueOn(today.AddDays(-1)).CompletedAt(noon).Build());

        var result = await WhenSlaQueried(clock);

        result.CurrentMonthPercentage.ShouldBe(50m);
        result.CurrentMonthOrderCount.ShouldBe(2);
    }

    [Test]
    public async Task Should_SeparateMetrics_For_CurrentAndLastMonth()
    {
        var clock = TestData.FutureClock();
        var userName = TestData.UserName("sla");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Test").WithLastName("User").WithEmail(TestData.Email(userName)).Build();
        var today = ChurchTimeZone.Today(clock);
        var lastMonth = today.AddMonths(-1);

        await GivenPersistedAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .WithNumber(TestData.Number()).DueOn(today)
                .CompletedAt(today.ToDateTime(new TimeOnly(12, 0))).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .WithNumber(TestData.Number()).DueOn(lastMonth)
                .CompletedAt(lastMonth.ToDateTime(new TimeOnly(12, 0))).Build());

        var result = await WhenSlaQueried(clock);

        result.CurrentMonthPercentage.ShouldBe(100m);
        result.CurrentMonthOrderCount.ShouldBe(1);
        result.LastMonthPercentage.ShouldBe(100m);
        result.LastMonthOrderCount.ShouldBe(1);
    }

    [Test]
    public async Task Should_FilterByCompletionMonth_NotDueMonth()
    {
        var clock = TestData.FutureClock();
        var userName = TestData.UserName("sla");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Test").WithLastName("User").WithEmail(TestData.Email(userName)).Build();
        var today = ChurchTimeZone.Today(clock);
        var order = WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(TestData.Number()).DueOn(today.AddMonths(1))
            .CompletedAt(today.ToDateTime(new TimeOnly(12, 0))).Build();

        await GivenPersistedAsync(creator, order);

        var result = await WhenSlaQueried(clock);

        // Should be counted in current month (when it was completed), and it's on time
        result.CurrentMonthPercentage.ShouldBe(100m);
        result.CurrentMonthOrderCount.ShouldBe(1);
    }

    [Test]
    public async Task ShouldReadOnlyTheTwoChurchMonths_InOneQuery()
    {
        var clock = TestData.FutureClock();
        var today = ChurchTimeZone.Today(clock);
        var window = GivenCompletionWindow(today);
        var userName = TestData.UserName("sla");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Test").WithLastName("User").WithEmail(TestData.Email(userName)).Build();
        var orders = SlaHealthSteps.BuildWindowOrders(creator, today, window.Start, window.Completed);

        await using var context = new CountingDataContext(TestHost.GetRequiredService<IDatabaseConfiguration>());
        context.Add(creator);
        context.AddRange(orders.AsEnumerable());
        await context.SaveChangesAsync();

        context.Commands.Reset();
        var result = await new SlaHealthQueryHandler(context, clock).Handle(new SlaHealthQuery());

        ThenOneWindowQuery(context, result, window);
    }

    private static async Task GivenPersistedAsync(Employee creator, params WorkOrder[] orders)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.AddRange(orders.Cast<object>());
        await context.SaveChangesAsync();
    }

    private static Task<SlaHealth> WhenSlaQueried(TimeProvider clock)
    {
        var handler = new SlaHealthQueryHandler(TestHost.GetRequiredService<DataContext>(), clock);
        return handler.Handle(new SlaHealthQuery()).AsTask();
    }

    private static (DateTime Start, DateTime End, DateTime Completed) GivenCompletionWindow(DateOnly today)
    {
        var currentMonthStart = new DateOnly(today.Year, today.Month, 1);
        var lastMonthStart = currentMonthStart.AddMonths(-1);
        var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
        var (windowStart, windowEnd) = SlaHealth.CompletionWindow(lastMonthStart, currentMonthEnd);
        return (windowStart, windowEnd, windowStart.AddHours(1));
    }

    private static void ThenOneWindowQuery(
        CountingDataContext context,
        SlaHealth result,
        (DateTime Start, DateTime End, DateTime Completed) window)
    {
        context.Commands.ReaderCommands.ShouldBe(1, string.Join(Environment.NewLine, context.Commands.Texts));
        context.Commands.OtherCommands.ShouldBe(0);
        var sql = context.Commands.Texts.Single();
        sql.ShouldContain("CompletedDate");
        sql.ShouldContain(">=");
        sql.ShouldContain("<");
        sql.ShouldNotContain("JOIN");
        window.Completed.ShouldBeGreaterThanOrEqualTo(window.Start);
        window.Completed.ShouldBeLessThan(window.End);
        result.LastMonthPercentage.ShouldBe(100m);
        result.LastMonthOrderCount.ShouldBe(1);
        result.CurrentMonthPercentage.ShouldBeNull();
        result.CurrentMonthOrderCount.ShouldBe(0);
    }
}
