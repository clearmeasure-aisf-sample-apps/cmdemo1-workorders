using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
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
        var creator = NewEmployee("sumcreator");
        await SaveAsync(creator,
            Order(creator, WorkOrderStatus.Draft, created: now.AddDays(-1)),
            Order(creator, WorkOrderStatus.Assigned, created: now.AddDays(-20), due: today.AddDays(-1)),
            Order(creator, WorkOrderStatus.InProgress, created: now.AddDays(-3), due: today),
            Order(creator, WorkOrderStatus.InProgress, created: now.AddDays(-40), due: today.AddDays(-10)),
            Order(creator, WorkOrderStatus.Complete, created: now.AddDays(-12), completed: now.AddDays(-10), due: today.AddDays(-30)),
            Order(creator, WorkOrderStatus.Complete, created: now.AddDays(-90), completed: now.AddDays(-60)),
            Order(creator, WorkOrderStatus.Cancelled, created: now.AddDays(-2), due: today.AddDays(-5)));

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
    public async Task ShouldCountWaitingOnVendorAsOpenAndInProgress()
    {
        var clock = TestData.FutureClock();
        var now = clock.GetUtcNow().UtcDateTime;
        var before = await Handle(null, clock);
        var creator = NewEmployee("sumcreator");
        var waiting = Order(creator, WorkOrderStatus.InProgress, created: now.AddDays(-20));
        waiting.WaitingOnVendorSince = now.AddDays(-1);
        await SaveAsync(creator, waiting, Order(creator, WorkOrderStatus.InProgress, created: now.AddDays(-20)));

        var summary = await Handle(null, clock);

        (summary.OpenCount - before.OpenCount).ShouldBe(2);
        (summary.InProgressCount - before.InProgressCount).ShouldBe(2);
    }

    [Test]
    public async Task ShouldCountAssignedToMeAndDueToday_ForUser()
    {
        var creator = NewEmployee("sumcreator2");
        var me = NewEmployee("summe");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(me);
            await context.SaveChangesAsync();
        }

        await SaveAsync(creator,
            Order(creator, WorkOrderStatus.Assigned, assignee: me, due: Today),
            Order(creator, WorkOrderStatus.InProgress, assignee: me, due: Today.AddDays(3)),
            Order(creator, WorkOrderStatus.Complete, assignee: me, due: Today),
            Order(creator, WorkOrderStatus.Assigned, assignee: creator, due: Today),
            Order(me, WorkOrderStatus.Draft));

        var summary = await Handle(me.UserName);

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
        var creator = NewEmployee("sumcreator3");
        var now = clock.GetUtcNow().UtcDateTime;
        await SaveAsync(creator,
            Order(creator, WorkOrderStatus.Complete, created: now.AddDays(-5), completed: now.AddDays(-3)),
            Order(creator, WorkOrderStatus.Complete, created: now.AddDays(-10), completed: now.AddDays(-6)),
            Order(creator, WorkOrderStatus.Complete, created: null, completed: now.AddDays(-1)));

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

    private static async Task<WorkOrderSearchSummary> Handle(string? userName, TimeProvider? clock = null)
    {
        var handler = new WorkOrderSearchSummaryQueryHandler(
            TestHost.GetRequiredService<DataContext>(), clock ?? TestData.FixedClock(Now));
        return await handler.Handle(new WorkOrderSearchSummaryQuery(userName));
    }

    private static Employee NewEmployee(string prefix)
    {
        var userName = TestData.UserName(prefix);
        return new Employee(userName, "Sum", "Tester", TestData.Email(userName));
    }

    private static WorkOrder Order(Employee creator, WorkOrderStatus status,
        Employee? assignee = null, DateTime? created = null, DateTime? completed = null, DateOnly? due = null) =>
        new()
        {
            Creator = creator, Assignee = assignee, Number = TestData.Number("S"), Title = $"Summary {creator.UserName}",
            Status = status,
            CreatedDate = created, CompletedDate = completed, DueDate = due
        };

    private static async Task SaveAsync(Employee creator, params WorkOrder[] orders)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        var assignees = orders.Select(o => o.Assignee).OfType<Employee>().Where(e => e != creator).Distinct();
        context.AttachRange(assignees);
        context.Add(creator);
        context.AddRange(orders.Cast<object>());
        await context.SaveChangesAsync();
    }
}
