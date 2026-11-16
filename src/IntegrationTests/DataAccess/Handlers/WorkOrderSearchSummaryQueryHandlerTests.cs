using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderSearchSummaryQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 13);

    [Test]
    public async Task ShouldCountOpenOverdueAndCompleted_WithFixedClock()
    {
        // The future clock isolates the 7- and 30-day windows; facility-wide open, overdue and
        // in-progress counts are compared with a snapshot taken before this test's rows exist.
        var clock = TestData.FutureClock();
        var now = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var before = await Handle(null, clock);
        await GivenOpenOverdueCompletedAsync(now, today);

        var summary = await Handle(null, clock);

        (summary.OpenCount - before.OpenCount).ShouldBe(4);
        summary.OpenedLast7Days.ShouldBe(2);
        (summary.OverdueCount - before.OverdueCount).ShouldBe(2);
        (summary.InProgressCount - before.InProgressCount).ShouldBe(2);
        summary.CompletedLast30Days.ShouldBe(1);
        summary.AssignedToMeCount.ShouldBe(0);
        summary.CreatedByMeCount.ShouldBe(0);
    }

    [Test]
    public async Task ShouldCountDueThisWeek_AroundTheSevenDayBoundary()
    {
        var clock = TestData.FutureClock();
        var today = ChurchTimeZone.Today(clock);
        var before = await Handle(null, clock);
        await GivenDueThisWeekAsync(today);

        var summary = await Handle(null, clock);

        (summary.DueThisWeekCount - before.DueThisWeekCount).ShouldBe(2);
    }

    [Test]
    public async Task ShouldCountWaitingOnVendorAsOpenAndInProgress()
    {
        var clock = TestData.FutureClock();
        var now = clock.GetUtcNow().UtcDateTime;
        var before = await Handle(null, clock);
        await GivenWaitingOnVendorAsync(now);

        var summary = await Handle(null, clock);

        (summary.OpenCount - before.OpenCount).ShouldBe(2);
        (summary.InProgressCount - before.InProgressCount).ShouldBe(2);
    }

    [Test]
    public async Task ShouldCountAssignedToMeAndDueToday_ForUser()
    {
        var meUserName = await GivenAssignedToMeAsync();

        var summary = await Handle(meUserName);

        summary.AssignedToMeCount.ShouldBe(2);
        summary.AssignedToMeTotal.ShouldBe(3);
        summary.AssignedToMeDueToday.ShouldBe(1);
        summary.CreatedByMeCount.ShouldBe(1);
        (await Handle(TestData.UserName("nobody"))).AssignedToMeTotal.ShouldBe(0);
    }

    [Test]
    public async Task ShouldComputeAverageDaysToClose()
    {
        var clock = TestData.FutureClock();
        await GivenAverageCloseAsync(clock);

        var summary = await Handle(null, clock);

        summary.CompletedLast30Days.ShouldBe(3);
        summary.AverageDaysToClose.ShouldNotBeNull();
        summary.AverageDaysToClose.Value.ShouldBe(3.0, 0.001);
    }

    [Test]
    public async Task ShouldReturnZeroWindowFiguresAndUserCounts_WhenNoOwnWorkOrders()
    {
        var summary = await Handle(TestData.UserName("anyone"), TestData.FutureClock());

        summary.OpenedLast7Days.ShouldBe(0);
        summary.CompletedLast30Days.ShouldBe(0);
        summary.AverageDaysToClose.ShouldBeNull();
        summary.AssignedToMeTotal.ShouldBe(0);
        summary.AssignedToMeCount.ShouldBe(0);
        summary.CreatedByMeCount.ShouldBe(0);
    }

    private static async Task GivenOpenOverdueCompletedAsync(DateTime now, DateOnly today)
    {
        var userName = TestData.UserName("sumcreator");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Sum").WithLastName("Tester").WithEmail(TestData.Email(userName)).Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).CreatedAt(now.AddDays(-1)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned)
                .CreatedAt(now.AddDays(-20)).DueOn(today.AddDays(-1)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress)
                .CreatedAt(now.AddDays(-3)).DueOn(today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress)
                .CreatedAt(now.AddDays(-40)).DueOn(today.AddDays(-10)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .CreatedAt(now.AddDays(-12)).CompletedAt(now.AddDays(-10)).DueOn(today.AddDays(-30)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .CreatedAt(now.AddDays(-90)).CompletedAt(now.AddDays(-60)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Cancelled)
                .CreatedAt(now.AddDays(-2)).DueOn(today.AddDays(-5)).Build());
    }

    private static async Task GivenDueThisWeekAsync(DateOnly today)
    {
        var userName = TestData.UserName("dueweek");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Sum").WithLastName("Tester").WithEmail(TestData.Email(userName)).Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).DueOn(today.AddDays(-1)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).DueOn(today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).DueOn(today.AddDays(6)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).DueOn(today.AddDays(7)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).DueOn(today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Cancelled).DueOn(today.AddDays(1)).Build());
    }

    private static async Task GivenWaitingOnVendorAsync(DateTime now)
    {
        var userName = TestData.UserName("sumcreator");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Sum").WithLastName("Tester").WithEmail(TestData.Email(userName)).Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress)
                .CreatedAt(now.AddDays(-20)).WaitingOnVendorSince(now.AddDays(-1)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).CreatedAt(now.AddDays(-20)).Build());
    }

    private static async Task<string> GivenAssignedToMeAsync()
    {
        var creatorName = TestData.UserName("sumcreator2");
        var meName = TestData.UserName("summe");
        var creator = EmployeeBuilder.Create(creatorName)
            .WithFirstName("Sum").WithLastName("Tester").WithEmail(TestData.Email(creatorName)).Build();
        var me = EmployeeBuilder.Create(meName)
            .WithFirstName("Sum").WithLastName("Tester").WithEmail(TestData.Email(meName)).Build();
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(me);
            await context.SaveChangesAsync();
        }

        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).AssignedTo(me).DueOn(Today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).AssignedTo(me).DueOn(Today.AddDays(3)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).AssignedTo(me).DueOn(Today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).AssignedTo(creator).DueOn(Today).Build(),
            WorkOrderBuilder.For(me, WorkOrderStatus.Draft).Build());
        return me.UserName;
    }

    private static async Task GivenAverageCloseAsync(TimeProvider clock)
    {
        var userName = TestData.UserName("sumcreator3");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Sum").WithLastName("Tester").WithEmail(TestData.Email(userName)).Build();
        var now = clock.GetUtcNow().UtcDateTime;
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .CreatedAt(now.AddDays(-5)).CompletedAt(now.AddDays(-3)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .CreatedAt(now.AddDays(-10)).CompletedAt(now.AddDays(-6)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .CompletedAt(now.AddDays(-1)).Build());
    }

    private static async Task<WorkOrderSearchSummary> Handle(string? userName, TimeProvider? clock = null)
    {
        var handler = new WorkOrderSearchSummaryQueryHandler(
            TestHost.GetRequiredService<DataContext>(), clock ?? TestData.FixedClock(Now));
        return await handler.Handle(new WorkOrderSearchSummaryQuery(userName));
    }
}
