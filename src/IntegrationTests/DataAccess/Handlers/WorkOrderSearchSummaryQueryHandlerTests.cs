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
        new DatabaseTests().Clean();
        var creator = new Employee("sumcreator", "Sum", "Creator", "s@example.com");
        var now = Now.DateTime;
        await SaveAsync(creator,
            Order(creator, "S000001", WorkOrderStatus.Draft, created: now.AddDays(-1)),
            Order(creator, "S000002", WorkOrderStatus.Assigned, created: now.AddDays(-20), due: Today.AddDays(-1)),
            Order(creator, "S000003", WorkOrderStatus.InProgress, created: now.AddDays(-3), due: Today),
            Order(creator, "S000004", WorkOrderStatus.InProgress, created: now.AddDays(-40), due: Today.AddDays(-10)),
            Order(creator, "S000005", WorkOrderStatus.Complete, created: now.AddDays(-12), completed: now.AddDays(-10), due: Today.AddDays(-30)),
            Order(creator, "S000006", WorkOrderStatus.Complete, created: now.AddDays(-90), completed: now.AddDays(-60)),
            Order(creator, "S000007", WorkOrderStatus.Cancelled, created: now.AddDays(-2), due: Today.AddDays(-5)));

        var summary = await Handle(null);

        summary.OpenCount.ShouldBe(4);
        summary.OpenedLast7Days.ShouldBe(2);
        summary.OverdueCount.ShouldBe(2);
        summary.InProgressCount.ShouldBe(2);
        summary.CompletedLast30Days.ShouldBe(1);
        summary.AssignedToMeCount.ShouldBe(0);
        summary.CreatedByMeCount.ShouldBe(0);
    }

    [Test]
    public async Task ShouldCountAssignedToMeAndDueToday_ForUser()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("sumcreator2", "Sum", "Creator", "s2@example.com");
        var me = new Employee("summe", "Sum", "Me", "me@example.com");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(me);
            await context.SaveChangesAsync();
        }

        await SaveAsync(creator,
            Order(creator, "U000001", WorkOrderStatus.Assigned, assignee: me, due: Today),
            Order(creator, "U000002", WorkOrderStatus.InProgress, assignee: me, due: Today.AddDays(3)),
            Order(creator, "U000003", WorkOrderStatus.Complete, assignee: me, due: Today),
            Order(creator, "U000004", WorkOrderStatus.Assigned, assignee: creator, due: Today),
            Order(me, "U000005", WorkOrderStatus.Draft));

        var summary = await Handle("summe");

        summary.AssignedToMeCount.ShouldBe(2);
        summary.AssignedToMeTotal.ShouldBe(3);
        summary.AssignedToMeDueToday.ShouldBe(1);
        summary.CreatedByMeCount.ShouldBe(1);
        (await Handle("nobody")).AssignedToMeTotal.ShouldBe(0);
    }

    [Test]
    public async Task ShouldComputeAverageDaysToClose()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("sumcreator3", "Sum", "Creator", "s3@example.com");
        var now = Now.DateTime;
        await SaveAsync(creator,
            Order(creator, "A000001", WorkOrderStatus.Complete, created: now.AddDays(-5), completed: now.AddDays(-3)),
            Order(creator, "A000002", WorkOrderStatus.Complete, created: now.AddDays(-10), completed: now.AddDays(-6)),
            Order(creator, "A000003", WorkOrderStatus.Complete, created: null, completed: now.AddDays(-1)));

        var summary = await Handle(null);

        summary.CompletedLast30Days.ShouldBe(3);
        summary.AverageDaysToClose.ShouldNotBeNull();
        summary.AverageDaysToClose.Value.ShouldBe(3.0, 0.001);
    }

    [Test]
    public async Task ShouldReturnZeros_WhenNoWorkOrders()
    {
        new DatabaseTests().Clean();

        var summary = await Handle("anyone");

        summary.OpenCount.ShouldBe(0);
        summary.OverdueCount.ShouldBe(0);
        summary.CompletedLast30Days.ShouldBe(0);
        summary.AverageDaysToClose.ShouldBeNull();
        summary.AssignedToMeTotal.ShouldBe(0);
    }

    private static async Task<WorkOrderSearchSummary> Handle(string? userName)
    {
        var handler = new WorkOrderSearchSummaryQueryHandler(
            TestHost.GetRequiredService<DataContext>(), new FixedClock(Now));
        return await handler.Handle(new WorkOrderSearchSummaryQuery(userName));
    }

    private static WorkOrder Order(Employee creator, string number, WorkOrderStatus status,
        Employee? assignee = null, DateTime? created = null, DateTime? completed = null, DateOnly? due = null) =>
        new()
        {
            Creator = creator, Assignee = assignee, Number = number, Title = number, Status = status,
            CreatedDate = created, CompletedDate = completed, DueDate = due
        };

    private static async Task SaveAsync(Employee creator, params WorkOrder[] orders)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        var assignees = orders.Select(o => o.Assignee).OfType<Employee>().Where(e => e != creator).Distinct();
        context.AttachRange(assignees);
        context.Add(creator);
        context.AddRange(orders);
        await context.SaveChangesAsync();
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
