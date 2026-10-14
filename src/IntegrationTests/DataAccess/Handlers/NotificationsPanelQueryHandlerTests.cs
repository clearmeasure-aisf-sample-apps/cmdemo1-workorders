using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
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
        var notifTestUserUserName = TestData.UserName("notif_test_user");
        var numberWo001 = TestData.Number("N");
        var numberWo002 = TestData.Number("N");
        var numberWo003 = TestData.Number("N");
        var numberWo004 = TestData.Number("N");

        var time = TestHost.GetRequiredService<TimeProvider>();
        var today = ChurchTimeZone.Today(time);
        var yesterday = today.AddDays(-1);
        var tomorrow = today.AddDays(1);
        var now = time.GetUtcNow().DateTime;

        var assignee = new Employee(notifTestUserUserName, "Test", "User", "test@example.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(assignee);

            var overdueOrder = new WorkOrder
            {
                Number = numberWo001,
                Title = "Overdue Task",
                Creator = assignee,
                Assignee = assignee,
                Status = WorkOrderStatus.Assigned,
                DueDate = yesterday,
                AssignedDate = now.AddDays(-10)
            };

            var dueToday = new WorkOrder
            {
                Number = numberWo002,
                Title = "Due Today Task",
                Creator = assignee,
                Assignee = assignee,
                Status = WorkOrderStatus.InProgress,
                DueDate = today,
                AssignedDate = now.AddDays(-5)
            };

            var futureDue = new WorkOrder
            {
                Number = numberWo003,
                Title = "Future Due Task",
                Creator = assignee,
                Assignee = assignee,
                Status = WorkOrderStatus.Assigned,
                DueDate = tomorrow,
                AssignedDate = now.AddDays(-3)
            };

            var complete = new WorkOrder
            {
                Number = numberWo004,
                Title = "Complete Task",
                Creator = assignee,
                Assignee = assignee,
                Status = WorkOrderStatus.Complete,
                DueDate = yesterday,
                CompletedDate = now
            };

            context.Add(overdueOrder);
            context.Add(dueToday);
            context.Add(futureDue);
            context.Add(complete);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new NotificationsPanelQueryHandler(dataContext, time);

        var result = await handler.Handle(new NotificationsPanelQuery(notifTestUserUserName));

        result.OverdueAndDueToday.Length.ShouldBe(2);
        result.OverdueAndDueToday.Select(wo => wo.Number).ShouldContain(numberWo001);
        result.OverdueAndDueToday.Select(wo => wo.Number).ShouldContain(numberWo002);
    }

    [Test]
    public async Task Handle_ReturnsNewlyAssignedLast7DaysWorkOrders()
    {
        var notifNewUserUserName = TestData.UserName("notif_new_user");
        var numberWo101 = TestData.Number("N");
        var numberWo102 = TestData.Number("N");
        var numberWo103 = TestData.Number("N");

        var time = TestHost.GetRequiredService<TimeProvider>();
        var now = time.GetUtcNow().DateTime;
        var today = ChurchTimeZone.Today(time);
        var futureDate = today.AddDays(10);

        var assignee = new Employee(notifNewUserUserName, "New", "User", "new@example.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(assignee);

            var recentlyAssigned = new WorkOrder
            {
                Number = numberWo101,
                Title = "Recent Assignment",
                Creator = assignee,
                Assignee = assignee,
                Status = WorkOrderStatus.Assigned,
                DueDate = futureDate,
                AssignedDate = now.AddDays(-3)
            };

            var oldAssignment = new WorkOrder
            {
                Number = numberWo102,
                Title = "Old Assignment",
                Creator = assignee,
                Assignee = assignee,
                Status = WorkOrderStatus.InProgress,
                DueDate = futureDate,
                AssignedDate = now.AddDays(-10)
            };

            var notAssigned = new WorkOrder
            {
                Number = numberWo103,
                Title = "Not Assigned",
                Creator = assignee,
                Assignee = null,
                Status = WorkOrderStatus.Draft,
                DueDate = futureDate
            };

            context.Add(recentlyAssigned);
            context.Add(oldAssignment);
            context.Add(notAssigned);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new NotificationsPanelQueryHandler(dataContext, time);

        var result = await handler.Handle(new NotificationsPanelQuery(notifNewUserUserName));

        result.NewlyAssignedLast7Days.Length.ShouldBe(1);
        result.NewlyAssignedLast7Days[0].Number.ShouldBe(numberWo101);
    }

    [Test]
    public async Task Handle_ReturnsCreatedByMeStatusChangedLast7DaysWorkOrders()
    {
        var notifCreatorUserName = TestData.UserName("notif_creator");
        var notifOtherUserName = TestData.UserName("notif_other");
        var numberWo201 = TestData.Number("N");
        var numberWo202 = TestData.Number("N");
        var numberWo203 = TestData.Number("N");
        var numberWo204 = TestData.Number("N");

        var time = TestHost.GetRequiredService<TimeProvider>();
        var now = time.GetUtcNow().DateTime;
        var today = ChurchTimeZone.Today(time);
        var futureDate = today.AddDays(30);

        var creator = new Employee(notifCreatorUserName, "Creator", "User", "creator@example.com");
        var otherUser = new Employee(notifOtherUserName, "Other", "User", "other@example.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(otherUser);

            var recentlyAssigned = new WorkOrder
            {
                Number = numberWo201,
                Title = "Recent Status Change",
                Creator = creator,
                Assignee = otherUser,
                Status = WorkOrderStatus.Assigned,
                DueDate = futureDate,
                AssignedDate = now.AddDays(-2)
            };

            var recentlyCompleted = new WorkOrder
            {
                Number = numberWo202,
                Title = "Recently Completed",
                Creator = creator,
                Assignee = otherUser,
                Status = WorkOrderStatus.Complete,
                DueDate = futureDate,
                CompletedDate = now.AddDays(-1)
            };

            var oldStatusChange = new WorkOrder
            {
                Number = numberWo203,
                Title = "Old Status Change",
                Creator = creator,
                Assignee = otherUser,
                Status = WorkOrderStatus.InProgress,
                DueDate = futureDate,
                AssignedDate = now.AddDays(-10)
            };

            var draft = new WorkOrder
            {
                Number = numberWo204,
                Title = "Draft Order",
                Creator = creator,
                Assignee = null,
                Status = WorkOrderStatus.Draft,
                DueDate = futureDate
            };

            context.Add(recentlyAssigned);
            context.Add(recentlyCompleted);
            context.Add(oldStatusChange);
            context.Add(draft);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new NotificationsPanelQueryHandler(dataContext, time);

        var result = await handler.Handle(new NotificationsPanelQuery(notifCreatorUserName));

        result.CreatedByMeStatusChangedLast7Days.Length.ShouldBe(2);
        var numbers = result.CreatedByMeStatusChangedLast7Days.Select(wo => wo.Number).ToList();
        numbers.ShouldContain(numberWo201);
        numbers.ShouldContain(numberWo202);
    }
}
