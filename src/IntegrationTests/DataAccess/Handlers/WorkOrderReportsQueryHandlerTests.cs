using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
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
        await GivenOpenByStatusAsync();

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
        await GivenOpenByPriorityAsync();

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
        await GivenOpenByBuildingAsync(sanctuary, kitchen);

        var data = await Handle();

        data.CountByBuilding[sanctuary].ShouldBe(3);
        data.CountByBuilding[kitchen].ShouldBe(1);
        data.CountByBuilding.Any(kvp => kvp.Key == null).ShouldBeFalse();
    }

    [Test]
    public async Task ShouldCountOverdueNow()
    {
        var before = await Handle();
        await GivenOverdueAsync();

        var data = await Handle();

        (data.OverdueCount - before.OverdueCount).ShouldBe(2);
    }

    [Test]
    public async Task ShouldComputeCompletedPerWeek_Last8Weeks()
    {
        var clock = TestData.FutureClock();
        await GivenCompletedPerWeekAsync(clock);

        var data = await Handle(clock);

        data.CompletedPerWeek.Count.ShouldBe(8);
        data.CompletedPerWeek.Sum(w => w.Count).ShouldBe(8);
        data.CompletedPerWeek.ShouldAllBe(w => w.Count == 1);
    }

    [Test]
    public async Task ShouldComputeAverageDaysToClose_Last30Days()
    {
        var clock = TestData.FutureClock();
        await GivenAverageDaysAsync(clock);

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
        await GivenMatchingAverageAsync(clock);

        var reportsHandler = new WorkOrderReportsQueryHandler(TestHost.GetRequiredService<DataContext>(), clock);
        var searchHandler = new WorkOrderSearchSummaryQueryHandler(TestHost.GetRequiredService<DataContext>(), clock);
        var reports = await reportsHandler.Handle(new WorkOrderReportsQuery());
        var search = await searchHandler.Handle(new WorkOrderSearchSummaryQuery(null));

        // (2 + 5 + 3) / 3 days
        reports.AverageDaysToClose.ShouldNotBeNull();
        reports.AverageDaysToClose.Value.ShouldBe(10.0 / 3, 0.001);
        reports.AverageDaysToClose.ShouldBe(search.AverageDaysToClose);
    }

    private static async Task GivenOpenByStatusAsync()
    {
        var userName = TestData.UserName("rcreator");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Reports").WithLastName("Tester").WithEmail(TestData.Email(userName)).Build();
        var now = Now.DateTime;
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).CreatedAt(Now.DateTime).DueOn(Today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).CreatedAt(Now.DateTime).DueOn(Today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).CreatedAt(Now.DateTime).DueOn(Today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).CreatedAt(Now.DateTime).DueOn(Today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).CreatedAt(Now.DateTime).DueOn(Today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .CreatedAt(Now.DateTime).CompletedAt(now.AddDays(-5)).DueOn(Today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Cancelled).CreatedAt(Now.DateTime).DueOn(Today).Build());
    }

    private static async Task GivenOpenByPriorityAsync()
    {
        var userName = TestData.UserName("pcreator");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Reports").WithLastName("Tester").WithEmail(TestData.Email(userName)).Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).CreatedAt(Now.DateTime).DueOn(Today)
                .WithPriority(WorkOrderPriority.High).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).CreatedAt(Now.DateTime).DueOn(Today)
                .WithPriority(WorkOrderPriority.High).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).CreatedAt(Now.DateTime).DueOn(Today)
                .WithPriority(WorkOrderPriority.Medium).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).CreatedAt(Now.DateTime).DueOn(Today)
                .WithPriority(WorkOrderPriority.Medium).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).CreatedAt(Now.DateTime).DueOn(Today)
                .WithPriority(WorkOrderPriority.Low).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(Now.DateTime).DueOn(Today)
                .WithPriority(WorkOrderPriority.High).Build());
    }

    private static async Task GivenOpenByBuildingAsync(string sanctuary, string kitchen)
    {
        var userName = TestData.UserName("bcreator");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Reports").WithLastName("Tester").WithEmail(TestData.Email(userName)).Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).CreatedAt(Now.DateTime).DueOn(Today)
                .InBuilding(sanctuary).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).CreatedAt(Now.DateTime).DueOn(Today)
                .InBuilding(sanctuary).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).CreatedAt(Now.DateTime).DueOn(Today)
                .InBuilding(kitchen).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).CreatedAt(Now.DateTime).DueOn(Today)
                .InBuilding(sanctuary).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).CreatedAt(Now.DateTime).DueOn(Today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(Now.DateTime).DueOn(Today)
                .InBuilding(sanctuary).Build());
    }

    private static async Task GivenOverdueAsync()
    {
        var userName = TestData.UserName("ocreator");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Reports").WithLastName("Tester").WithEmail(TestData.Email(userName)).Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).CreatedAt(Now.DateTime).DueOn(Today.AddDays(-1)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).CreatedAt(Now.DateTime).DueOn(Today.AddDays(-5)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).CreatedAt(Now.DateTime).DueOn(Today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).CreatedAt(Now.DateTime).DueOn(Today.AddDays(1)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(Now.DateTime).DueOn(Today.AddDays(-1)).Build());
    }

    private static async Task GivenCompletedPerWeekAsync(TimeProvider clock)
    {
        var userName = TestData.UserName("wcreator");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Reports").WithLastName("Tester").WithEmail(TestData.Email(userName)).Build();
        // One completion in each of the last 8 weeks; the two at day -56 fall before the first week shown
        await WorkOrderQuerySteps.SaveOrdersAsync(creator, CompletedWeekOrders(creator, clock.GetUtcNow().UtcDateTime));
    }

    private static WorkOrder[] CompletedWeekOrders(Employee creator, DateTime now) =>
    [
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(Now.DateTime).DueOn(Today)
            .CompletedAt(now.AddDays(-56)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(Now.DateTime).DueOn(Today)
            .CompletedAt(now.AddDays(-56)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(Now.DateTime).DueOn(Today)
            .CompletedAt(now.AddDays(-49)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(Now.DateTime).DueOn(Today)
            .CompletedAt(now.AddDays(-42)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(Now.DateTime).DueOn(Today)
            .CompletedAt(now.AddDays(-35)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(Now.DateTime).DueOn(Today)
            .CompletedAt(now.AddDays(-28)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(Now.DateTime).DueOn(Today)
            .CompletedAt(now.AddDays(-21)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(Now.DateTime).DueOn(Today)
            .CompletedAt(now.AddDays(-14)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(Now.DateTime).DueOn(Today)
            .CompletedAt(now.AddDays(-7)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(Now.DateTime).DueOn(Today)
            .CompletedAt(now).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).CreatedAt(Now.DateTime).DueOn(Today).Build()
    ];


    private static async Task GivenAverageDaysAsync(TimeProvider clock)
    {
        var userName = TestData.UserName("acreator");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Reports").WithLastName("Tester").WithEmail(TestData.Email(userName)).Build();
        var now = clock.GetUtcNow().UtcDateTime;
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .CreatedAt(now.AddDays(-10)).CompletedAt(now.AddDays(-5)).DueOn(Today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .CreatedAt(now.AddDays(-8)).CompletedAt(now.AddDays(-2)).DueOn(Today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).CreatedAt(Now.DateTime).DueOn(Today).Build());
    }

    private static async Task GivenMatchingAverageAsync(TimeProvider clock)
    {
        var userName = TestData.UserName("screator");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Reports").WithLastName("Tester").WithEmail(TestData.Email(userName)).Build();
        var now = clock.GetUtcNow().UtcDateTime;
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .CreatedAt(now.AddDays(-12)).CompletedAt(now.AddDays(-10)).DueOn(Today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .CreatedAt(now.AddDays(-8)).CompletedAt(now.AddDays(-3)).DueOn(Today).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
                .CreatedAt(now.AddDays(-4)).CompletedAt(now.AddDays(-1)).DueOn(Today).Build());
    }

    private static async Task<WorkOrderReportsData> Handle(TimeProvider? clock = null)
    {
        var handler = new WorkOrderReportsQueryHandler(
            TestHost.GetRequiredService<DataContext>(), clock ?? TestData.FixedClock(Now));
        return await handler.Handle(new WorkOrderReportsQuery());
    }

    private static int Delta(IReadOnlyDictionary<string, int> before, IReadOnlyDictionary<string, int> after, string key) =>
        after.GetValueOrDefault(key) - before.GetValueOrDefault(key);
}
