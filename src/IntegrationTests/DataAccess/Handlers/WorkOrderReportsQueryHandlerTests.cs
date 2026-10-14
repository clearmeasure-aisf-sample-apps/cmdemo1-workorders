using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderReportsQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 13);

    [Test]
    public async Task ShouldCountOpenByStatus()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("rcreator", "Reports", "Creator", "r@example.com");
        var now = Now.DateTime;
        await SaveAsync(creator,
            Order(creator, "R001", WorkOrderStatus.Draft),
            Order(creator, "R002", WorkOrderStatus.Draft),
            Order(creator, "R003", WorkOrderStatus.Assigned),
            Order(creator, "R004", WorkOrderStatus.Assigned),
            Order(creator, "R005", WorkOrderStatus.InProgress),
            Order(creator, "R006", WorkOrderStatus.Complete, completed: now.AddDays(-5)),
            Order(creator, "R007", WorkOrderStatus.Cancelled));

        var data = await Handle();

        data.CountByStatus["Draft"].ShouldBe(2);
        data.CountByStatus["Assigned"].ShouldBe(2);
        data.CountByStatus["InProgress"].ShouldBe(1);
        data.CountByStatus.ContainsKey("Complete").ShouldBeFalse();
    }

    [Test]
    public async Task ShouldCountOpenByPriority()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("pcreator", "Priority", "Creator", "p@example.com");
        await SaveAsync(creator,
            Order(creator, "P001", WorkOrderStatus.Draft, priority: WorkOrderPriority.High),
            Order(creator, "P002", WorkOrderStatus.Draft, priority: WorkOrderPriority.High),
            Order(creator, "P003", WorkOrderStatus.Assigned, priority: WorkOrderPriority.Medium),
            Order(creator, "P004", WorkOrderStatus.InProgress, priority: WorkOrderPriority.Medium),
            Order(creator, "P005", WorkOrderStatus.Assigned, priority: WorkOrderPriority.Low),
            Order(creator, "P006", WorkOrderStatus.Complete, priority: WorkOrderPriority.High));

        var data = await Handle();

        data.CountByPriority["High"].ShouldBe(2);
        data.CountByPriority["Medium"].ShouldBe(2);
        data.CountByPriority["Low"].ShouldBe(1);
    }

    [Test]
    public async Task ShouldCountOpenByBuilding()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("bcreator", "Building", "Creator", "b@example.com");
        await SaveAsync(creator,
            Order(creator, "B001", WorkOrderStatus.Draft, building: "Sanctuary"),
            Order(creator, "B002", WorkOrderStatus.Draft, building: "Sanctuary"),
            Order(creator, "B003", WorkOrderStatus.Assigned, building: "Kitchen"),
            Order(creator, "B004", WorkOrderStatus.Assigned, building: "Sanctuary"),
            Order(creator, "B005", WorkOrderStatus.InProgress, building: null),
            Order(creator, "B006", WorkOrderStatus.Complete, building: "Sanctuary"));

        var data = await Handle();

        data.CountByBuilding["Sanctuary"].ShouldBe(3);
        data.CountByBuilding["Kitchen"].ShouldBe(1);
        data.CountByBuilding.Any(kvp => kvp.Key == null).ShouldBeFalse();
    }

    [Test]
    public async Task ShouldCountOverdueNow()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("ocreator", "Overdue", "Creator", "o@example.com");
        await SaveAsync(creator,
            Order(creator, "O001", WorkOrderStatus.Draft, due: Today.AddDays(-1)),
            Order(creator, "O002", WorkOrderStatus.Assigned, due: Today.AddDays(-5)),
            Order(creator, "O003", WorkOrderStatus.InProgress, due: Today),
            Order(creator, "O004", WorkOrderStatus.InProgress, due: Today.AddDays(1)),
            Order(creator, "O005", WorkOrderStatus.Complete, due: Today.AddDays(-1)));

        var data = await Handle();

        data.OverdueCount.ShouldBe(2);
    }

    [Test]
    public async Task ShouldComputeCompletedPerWeek_Last8Weeks()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("wcreator", "Weekly", "Creator", "w@example.com");
        var now = Now.DateTime;

        // Seed work orders across 8 weeks
        await SaveAsync(creator,
            Order(creator, "W001", WorkOrderStatus.Complete, completed: now.AddDays(-56)),
            Order(creator, "W002", WorkOrderStatus.Complete, completed: now.AddDays(-56)),
            Order(creator, "W003", WorkOrderStatus.Complete, completed: now.AddDays(-49)),
            Order(creator, "W004", WorkOrderStatus.Complete, completed: now.AddDays(-42)),
            Order(creator, "W005", WorkOrderStatus.Complete, completed: now.AddDays(-35)),
            Order(creator, "W006", WorkOrderStatus.Complete, completed: now.AddDays(-28)),
            Order(creator, "W007", WorkOrderStatus.Complete, completed: now.AddDays(-21)),
            Order(creator, "W008", WorkOrderStatus.Complete, completed: now.AddDays(-14)),
            Order(creator, "W009", WorkOrderStatus.Complete, completed: now.AddDays(-7)),
            Order(creator, "W010", WorkOrderStatus.Complete, completed: now),
            Order(creator, "W011", WorkOrderStatus.Draft));

        var data = await Handle();

        data.CompletedPerWeek.Count.ShouldBe(8);
        data.CompletedPerWeek.Sum(w => w.Count).ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task ShouldComputeAverageDaysToClose_Last30Days()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("acreator", "Average", "Creator", "a@example.com");
        var now = Now.DateTime;

        await SaveAsync(creator,
            Order(creator, "A001", WorkOrderStatus.Complete, created: now.AddDays(-10), completed: now.AddDays(-5)),
            Order(creator, "A002", WorkOrderStatus.Complete, created: now.AddDays(-8), completed: now.AddDays(-2)),
            Order(creator, "A004", WorkOrderStatus.Draft));

        var data = await Handle();

        data.CompletedLast30Days.ShouldBe(2);
        data.AverageDaysToClose.ShouldNotBeNull();
        // (10-5) + (8-2) = 5 + 6 = 11 days total / 2 = 5.5 days average
        data.AverageDaysToClose.Value.ShouldBe(5.5, 0.001);
    }

    [Test]
    public async Task ShouldReturnZeros_WhenNoData()
    {
        new DatabaseTests().Clean();

        var data = await Handle();

        data.CountByStatus.Count.ShouldBe(0);
        data.CountByPriority.Count.ShouldBe(0);
        data.CountByBuilding.Count.ShouldBe(0);
        data.CompletedPerWeek.Count.ShouldBe(8);
        data.CompletedPerWeek.All(w => w.Count == 0).ShouldBeTrue();
        data.AverageDaysToClose.ShouldBeNull();
        data.OverdueCount.ShouldBe(0);
    }

    [Test]
    public async Task AverageDaysToClose_ShouldMatchSearchPageLogic()
    {
        // Verify that the Reports query uses the same averaging logic as the search page
        new DatabaseTests().Clean();
        var creator = new Employee("screator", "Sync", "Creator", "s@example.com");
        var now = Now.DateTime;

        await SaveAsync(creator,
            Order(creator, "S001", WorkOrderStatus.Complete, created: now.AddDays(-12), completed: now.AddDays(-10)),
            Order(creator, "S002", WorkOrderStatus.Complete, created: now.AddDays(-8), completed: now.AddDays(-3)),
            Order(creator, "S003", WorkOrderStatus.Complete, created: now.AddDays(-4), completed: now.AddDays(-1)));

        var clock = new FixedClock(Now);
        var reportsHandler = new WorkOrderReportsQueryHandler(TestHost.GetRequiredService<DataContext>(), clock);
        var searchHandler = new WorkOrderSearchSummaryQueryHandler(TestHost.GetRequiredService<DataContext>(), clock);

        var reports = await reportsHandler.Handle(new WorkOrderReportsQuery());
        var search = await searchHandler.Handle(new WorkOrderSearchSummaryQuery(null));

        reports.AverageDaysToClose.ShouldBe(search.AverageDaysToClose);
    }

    private static async Task<WorkOrderReportsData> Handle()
    {
        var handler = new WorkOrderReportsQueryHandler(
            TestHost.GetRequiredService<DataContext>(), new FixedClock(Now));
        return await handler.Handle(new WorkOrderReportsQuery());
    }

    private static WorkOrder Order(Employee creator, string number, WorkOrderStatus status,
        Employee? assignee = null, DateTime? created = null, DateTime? completed = null, DateOnly? due = null,
        WorkOrderPriority? priority = null, string? building = null) =>
        new()
        {
            Creator = creator,
            Assignee = assignee,
            Number = number,
            Title = number,
            Status = status,
            CreatedDate = created ?? Now.DateTime,
            CompletedDate = completed,
            DueDate = due ?? Today,
            Priority = priority ?? WorkOrderPriority.Medium,
            Building = building
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

    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
