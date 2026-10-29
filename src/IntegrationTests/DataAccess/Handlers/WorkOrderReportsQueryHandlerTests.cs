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
        var before = await Handle();
        var creator = NewEmployee("rcreator");
        var now = Now.DateTime;
        await SaveAsync(creator,
            Order(creator, WorkOrderStatus.Draft),
            Order(creator, WorkOrderStatus.Draft),
            Order(creator, WorkOrderStatus.Assigned),
            Order(creator, WorkOrderStatus.Assigned),
            Order(creator, WorkOrderStatus.InProgress),
            Order(creator, WorkOrderStatus.Complete, completed: now.AddDays(-5)),
            Order(creator, WorkOrderStatus.Cancelled));

        var data = await Handle();

        Delta(before.CountByStatus, data.CountByStatus, "Draft").ShouldBe(2);
        Delta(before.CountByStatus, data.CountByStatus, "Assigned").ShouldBe(2);
        Delta(before.CountByStatus, data.CountByStatus, "InProgress").ShouldBe(1);
        data.CountByStatus.ContainsKey("Complete").ShouldBeFalse();
    }

    [Test]
    public async Task ShouldCountOpenByPriority()
    {
        var before = await Handle();
        var creator = NewEmployee("pcreator");
        await SaveAsync(creator,
            Order(creator, WorkOrderStatus.Draft, priority: WorkOrderPriority.High),
            Order(creator, WorkOrderStatus.Draft, priority: WorkOrderPriority.High),
            Order(creator, WorkOrderStatus.Assigned, priority: WorkOrderPriority.Medium),
            Order(creator, WorkOrderStatus.InProgress, priority: WorkOrderPriority.Medium),
            Order(creator, WorkOrderStatus.Assigned, priority: WorkOrderPriority.Low),
            Order(creator, WorkOrderStatus.Complete, priority: WorkOrderPriority.High));

        var data = await Handle();

        Delta(before.CountByPriority, data.CountByPriority, "High").ShouldBe(2);
        Delta(before.CountByPriority, data.CountByPriority, "Medium").ShouldBe(2);
        Delta(before.CountByPriority, data.CountByPriority, "Low").ShouldBe(1);
    }

    [Test]
    public async Task ShouldCountOpenByBuilding()
    {
        var tag = TestData.Tag();
        var sanctuary = $"Sanctuary {tag}";
        var kitchen = $"Kitchen {tag}";
        var creator = NewEmployee("bcreator");
        await SaveAsync(creator,
            Order(creator, WorkOrderStatus.Draft, building: sanctuary),
            Order(creator, WorkOrderStatus.Draft, building: sanctuary),
            Order(creator, WorkOrderStatus.Assigned, building: kitchen),
            Order(creator, WorkOrderStatus.Assigned, building: sanctuary),
            Order(creator, WorkOrderStatus.InProgress, building: null),
            Order(creator, WorkOrderStatus.Complete, building: sanctuary));

        var data = await Handle();

        data.CountByBuilding[sanctuary].ShouldBe(3);
        data.CountByBuilding[kitchen].ShouldBe(1);
        data.CountByBuilding.Any(kvp => kvp.Key == null).ShouldBeFalse();
    }

    [Test]
    public async Task ShouldCountOverdueNow()
    {
        var before = await Handle();
        var creator = NewEmployee("ocreator");
        await SaveAsync(creator,
            Order(creator, WorkOrderStatus.Draft, due: Today.AddDays(-1)),
            Order(creator, WorkOrderStatus.Assigned, due: Today.AddDays(-5)),
            Order(creator, WorkOrderStatus.InProgress, due: Today),
            Order(creator, WorkOrderStatus.InProgress, due: Today.AddDays(1)),
            Order(creator, WorkOrderStatus.Complete, due: Today.AddDays(-1)));

        var data = await Handle();

        (data.OverdueCount - before.OverdueCount).ShouldBe(2);
    }

    [Test]
    public async Task ShouldComputeCompletedPerWeek_Last8Weeks()
    {
        var clock = TestData.FutureClock();
        var creator = NewEmployee("wcreator");
        var now = clock.GetUtcNow().UtcDateTime;

        // One completion in each of the last 8 weeks; the two at day -56 fall before the first week shown
        await SaveAsync(creator,
            Order(creator, WorkOrderStatus.Complete, completed: now.AddDays(-56)),
            Order(creator, WorkOrderStatus.Complete, completed: now.AddDays(-56)),
            Order(creator, WorkOrderStatus.Complete, completed: now.AddDays(-49)),
            Order(creator, WorkOrderStatus.Complete, completed: now.AddDays(-42)),
            Order(creator, WorkOrderStatus.Complete, completed: now.AddDays(-35)),
            Order(creator, WorkOrderStatus.Complete, completed: now.AddDays(-28)),
            Order(creator, WorkOrderStatus.Complete, completed: now.AddDays(-21)),
            Order(creator, WorkOrderStatus.Complete, completed: now.AddDays(-14)),
            Order(creator, WorkOrderStatus.Complete, completed: now.AddDays(-7)),
            Order(creator, WorkOrderStatus.Complete, completed: now),
            Order(creator, WorkOrderStatus.Draft));

        var data = await Handle(clock);

        data.CompletedPerWeek.Count.ShouldBe(8);
        data.CompletedPerWeek.Sum(w => w.Count).ShouldBe(8);
        data.CompletedPerWeek.ShouldAllBe(w => w.Count == 1);
    }

    [Test]
    public async Task ShouldComputeAverageDaysToClose_Last30Days()
    {
        var clock = TestData.FutureClock();
        var creator = NewEmployee("acreator");
        var now = clock.GetUtcNow().UtcDateTime;

        await SaveAsync(creator,
            Order(creator, WorkOrderStatus.Complete, created: now.AddDays(-10), completed: now.AddDays(-5)),
            Order(creator, WorkOrderStatus.Complete, created: now.AddDays(-8), completed: now.AddDays(-2)),
            Order(creator, WorkOrderStatus.Draft));

        var data = await Handle(clock);

        data.CompletedLast30Days.ShouldBe(2);
        data.AverageDaysToClose.ShouldNotBeNull();
        // (10-5) + (8-2) = 5 + 6 = 11 days total / 2 = 5.5 days average
        data.AverageDaysToClose.Value.ShouldBe(5.5, 0.001);
    }

    [Test]
    public async Task ShouldReturnZeroWindowFigures_WhenNoOwnData()
    {
        var data = await Handle(TestData.FutureClock());

        data.CompletedPerWeek.Count.ShouldBe(8);
        data.CompletedPerWeek.All(w => w.Count == 0).ShouldBeTrue();
        data.CompletedLast30Days.ShouldBe(0);
        data.AverageDaysToClose.ShouldBeNull();
    }

    [Test]
    public async Task AverageDaysToClose_ShouldMatchSearchPageLogic()
    {
        // Verify that the Reports query uses the same averaging logic as the search page
        var clock = TestData.FutureClock();
        var creator = NewEmployee("screator");
        var now = clock.GetUtcNow().UtcDateTime;

        await SaveAsync(creator,
            Order(creator, WorkOrderStatus.Complete, created: now.AddDays(-12), completed: now.AddDays(-10)),
            Order(creator, WorkOrderStatus.Complete, created: now.AddDays(-8), completed: now.AddDays(-3)),
            Order(creator, WorkOrderStatus.Complete, created: now.AddDays(-4), completed: now.AddDays(-1)));

        var reportsHandler = new WorkOrderReportsQueryHandler(TestHost.GetRequiredService<DataContext>(), clock);
        var searchHandler = new WorkOrderSearchSummaryQueryHandler(TestHost.GetRequiredService<DataContext>(), clock);

        var reports = await reportsHandler.Handle(new WorkOrderReportsQuery());
        var search = await searchHandler.Handle(new WorkOrderSearchSummaryQuery(null));

        // (2 + 5 + 3) / 3 days
        reports.AverageDaysToClose.ShouldNotBeNull();
        reports.AverageDaysToClose.Value.ShouldBe(10.0 / 3, 0.001);
        reports.AverageDaysToClose.ShouldBe(search.AverageDaysToClose);
    }

    private static async Task<WorkOrderReportsData> Handle(TimeProvider? clock = null)
    {
        var handler = new WorkOrderReportsQueryHandler(
            TestHost.GetRequiredService<DataContext>(), clock ?? TestData.FixedClock(Now));
        return await handler.Handle(new WorkOrderReportsQuery());
    }

    private static int Delta(IReadOnlyDictionary<string, int> before, IReadOnlyDictionary<string, int> after, string key) =>
        after.GetValueOrDefault(key) - before.GetValueOrDefault(key);

    private static Employee NewEmployee(string prefix)
    {
        var userName = TestData.UserName(prefix);
        return new Employee(userName, "Reports", "Tester", TestData.Email(userName));
    }

    private static WorkOrder Order(Employee creator, WorkOrderStatus status,
        Employee? assignee = null, DateTime? created = null, DateTime? completed = null, DateOnly? due = null,
        WorkOrderPriority? priority = null, string? building = null) =>
        new()
        {
            Creator = creator,
            Assignee = assignee,
            Number = TestData.Number("R"),
            Title = $"Reports {creator.UserName}",
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
}
