using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class NotificationsPanelQueryHandlerTests
{
    [Test]
    public async Task Handle_WithNullUserName_ReturnsEmptyArrays()
    {
        var dataContext = TestHost.GetRequiredService<DataContext>();
        var time = TestHost.GetRequiredService<TimeProvider>();
        var handler = new NotificationsPanelQueryHandler(dataContext, time);

        var result = await handler.Handle(new NotificationsPanelQuery(null));

        result.OverdueAndDueToday.Length.ShouldBe(0);
        result.NewlyAssignedLast7Days.Length.ShouldBe(0);
        result.CreatedByMeStatusChangedLast7Days.Length.ShouldBe(0);
    }

    [Test]
    public async Task Handle_WithNonExistentUserName_ReturnsEmptyArrays()
    {
        var nonexistentUserUserName = TestData.UserName("nonexistent_user");

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var time = TestHost.GetRequiredService<TimeProvider>();
        var handler = new NotificationsPanelQueryHandler(dataContext, time);

        var result = await handler.Handle(new NotificationsPanelQuery(nonexistentUserUserName));

        result.OverdueAndDueToday.Length.ShouldBe(0);
        result.NewlyAssignedLast7Days.Length.ShouldBe(0);
        result.CreatedByMeStatusChangedLast7Days.Length.ShouldBe(0);
    }

    [Test]
    public async Task Handle_ReturnsOverdueAndDueTodayWorkOrders()
    {
        var userName = TestData.UserName("notif_test_user");
        var expected = await GivenOverdueAndDueTodayAsync(userName);

        var result = await WhenQueriedAsync(userName);

        result.OverdueAndDueToday.Length.ShouldBe(2);
        result.OverdueAndDueToday.Select(wo => wo.Number).ShouldContain(expected[0]);
        result.OverdueAndDueToday.Select(wo => wo.Number).ShouldContain(expected[1]);
    }

    [Test]
    public async Task Handle_ReturnsNewlyAssignedLast7DaysWorkOrders()
    {
        var userName = TestData.UserName("notif_new_user");
        var recentNumber = await GivenNewlyAssignedAsync(userName);

        var result = await WhenQueriedAsync(userName);

        result.NewlyAssignedLast7Days.Length.ShouldBe(1);
        result.NewlyAssignedLast7Days[0].Number.ShouldBe(recentNumber);
    }

    [Test]
    public async Task Handle_ReturnsCreatedByMeStatusChangedLast7DaysWorkOrders()
    {
        var creatorName = TestData.UserName("notif_creator");
        var expected = await GivenCreatedByMeStatusChangedAsync(creatorName);

        var result = await WhenQueriedAsync(creatorName);

        result.CreatedByMeStatusChangedLast7Days.Length.ShouldBe(2);
        var numbers = result.CreatedByMeStatusChangedLast7Days.Select(wo => wo.Number).ToList();
        numbers.ShouldContain(expected[0]);
        numbers.ShouldContain(expected[1]);
    }

    private static async Task<string[]> GivenOverdueAndDueTodayAsync(string userName)
    {
        var time = TestHost.GetRequiredService<TimeProvider>();
        var today = ChurchTimeZone.Today(time);
        var now = time.GetUtcNow().DateTime;
        var assignee = EmployeeBuilder.Create(userName)
            .WithFirstName("Test").WithLastName("User").WithEmail("test@example.com").Build();
        var overdue = TestData.Number("N");
        var dueToday = TestData.Number("N");
        await WorkOrderQuerySteps.SaveOrdersAsync(assignee,
            WorkOrderBuilder.For(assignee, WorkOrderStatus.Assigned).WithNumber(overdue)
                .WithTitle("Overdue Task").AssignedTo(assignee).DueOn(today.AddDays(-1))
                .AssignedAt(now.AddDays(-10)).Build(),
            WorkOrderBuilder.For(assignee, WorkOrderStatus.InProgress).WithNumber(dueToday)
                .WithTitle("Due Today Task").AssignedTo(assignee).DueOn(today)
                .AssignedAt(now.AddDays(-5)).Build(),
            WorkOrderBuilder.For(assignee, WorkOrderStatus.Assigned).WithNumber(TestData.Number("N"))
                .WithTitle("Future Due Task").AssignedTo(assignee).DueOn(today.AddDays(1))
                .AssignedAt(now.AddDays(-3)).Build(),
            WorkOrderBuilder.For(assignee, WorkOrderStatus.Complete).WithNumber(TestData.Number("N"))
                .WithTitle("Complete Task").AssignedTo(assignee).DueOn(today.AddDays(-1))
                .CompletedAt(now).Build());
        return [overdue, dueToday];
    }

    private static async Task<string> GivenNewlyAssignedAsync(string userName)
    {
        var time = TestHost.GetRequiredService<TimeProvider>();
        var now = time.GetUtcNow().DateTime;
        var futureDate = ChurchTimeZone.Today(time).AddDays(10);
        var assignee = EmployeeBuilder.Create(userName)
            .WithFirstName("New").WithLastName("User").WithEmail("new@example.com").Build();
        var recent = TestData.Number("N");
        await WorkOrderQuerySteps.SaveOrdersAsync(assignee,
            WorkOrderBuilder.For(assignee, WorkOrderStatus.Assigned).WithNumber(recent)
                .WithTitle("Recent Assignment").AssignedTo(assignee).DueOn(futureDate)
                .AssignedAt(now.AddDays(-3)).Build(),
            WorkOrderBuilder.For(assignee, WorkOrderStatus.InProgress).WithNumber(TestData.Number("N"))
                .WithTitle("Old Assignment").AssignedTo(assignee).DueOn(futureDate)
                .AssignedAt(now.AddDays(-10)).Build(),
            WorkOrderBuilder.For(assignee, WorkOrderStatus.Draft).WithNumber(TestData.Number("N"))
                .WithTitle("Not Assigned").DueOn(futureDate).Build());
        return recent;
    }

    private static async Task<string[]> GivenCreatedByMeStatusChangedAsync(string creatorName)
    {
        var time = TestHost.GetRequiredService<TimeProvider>();
        var now = time.GetUtcNow().DateTime;
        var futureDate = ChurchTimeZone.Today(time).AddDays(30);
        var creator = EmployeeBuilder.Create(creatorName)
            .WithFirstName("Creator").WithLastName("User").WithEmail("creator@example.com").Build();
        var other = EmployeeBuilder.Create(TestData.UserName("notif_other"))
            .WithFirstName("Other").WithLastName("User").WithEmail("other@example.com").Build();
        var assigned = TestData.Number("N");
        var completed = TestData.Number("N");
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).WithNumber(assigned)
                .WithTitle("Recent Status Change").AssignedTo(other).DueOn(futureDate)
                .AssignedAt(now.AddDays(-2)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).WithNumber(completed)
                .WithTitle("Recently Completed").AssignedTo(other).DueOn(futureDate)
                .CompletedAt(now.AddDays(-1)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).WithNumber(TestData.Number("N"))
                .WithTitle("Old Status Change").AssignedTo(other).DueOn(futureDate)
                .AssignedAt(now.AddDays(-10)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).WithNumber(TestData.Number("N"))
                .WithTitle("Draft Order").DueOn(futureDate).Build());
        return [assigned, completed];
    }

    private static Task<NotificationsSummary> WhenQueriedAsync(string userName)
    {
        var handler = new NotificationsPanelQueryHandler(
            TestHost.GetRequiredService<DataContext>(),
            TestHost.GetRequiredService<TimeProvider>());
        return handler.Handle(new NotificationsPanelQuery(userName)).AsTask();
    }
}
