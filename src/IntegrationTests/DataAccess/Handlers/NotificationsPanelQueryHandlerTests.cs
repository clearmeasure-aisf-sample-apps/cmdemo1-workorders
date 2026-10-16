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
        var overdueOrderNumber = TestData.Number("N");
        var dueTodayNumber = TestData.Number("N");
        var futureDueNumber = TestData.Number("N");
        var completeNumber = TestData.Number("N");

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
                Number = overdueOrderNumber,
                Title = "Overdue Task",
                Creator = assignee,
                Assignee = assignee,
                Status = WorkOrderStatus.Assigned,
                DueDate = yesterday,
                AssignedDate = now.AddDays(-10)
            };

            var dueToday = new WorkOrder
            {
                Number = dueTodayNumber,
                Title = "Due Today Task",
                Creator = assignee,
                Assignee = assignee,
                Status = WorkOrderStatus.InProgress,
                DueDate = today,
                AssignedDate = now.AddDays(-5)
            };

            var futureDue = new WorkOrder
            {
                Number = futureDueNumber,
                Title = "Future Due Task",
                Creator = assignee,
                Assignee = assignee,
                Status = WorkOrderStatus.Assigned,
                DueDate = tomorrow,
                AssignedDate = now.AddDays(-3)
            };

            var complete = new WorkOrder
            {
                Number = completeNumber,
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
        result.OverdueAndDueToday.Select(wo => wo.Number).ShouldContain(overdueOrderNumber);
        result.OverdueAndDueToday.Select(wo => wo.Number).ShouldContain(dueTodayNumber);
    }

    [Test]
    public async Task Handle_ReturnsNewlyAssignedLast7DaysWorkOrders()
    {
        var notifNewUserUserName = TestData.UserName("notif_new_user");
        var recentlyAssignedNumber = TestData.Number("N");
        var oldAssignmentNumber = TestData.Number("N");
        var notAssignedNumber = TestData.Number("N");

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
                Number = recentlyAssignedNumber,
                Title = "Recent Assignment",
                Creator = assignee,
                Assignee = assignee,
                Status = WorkOrderStatus.Assigned,
                DueDate = futureDate,
                AssignedDate = now.AddDays(-3)
            };

            var oldAssignment = new WorkOrder
            {
                Number = oldAssignmentNumber,
                Title = "Old Assignment",
                Creator = assignee,
                Assignee = assignee,
                Status = WorkOrderStatus.InProgress,
                DueDate = futureDate,
                AssignedDate = now.AddDays(-10)
            };

            var notAssigned = new WorkOrder
            {
                Number = notAssignedNumber,
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
        result.NewlyAssignedLast7Days[0].Number.ShouldBe(recentlyAssignedNumber);
    }

    [Test]
    public async Task Handle_ReturnsCreatedByMeStatusChangedLast7DaysWorkOrders()
    {
        var notifCreatorUserName = TestData.UserName("notif_creator");
        var notifOtherUserName = TestData.UserName("notif_other");
        var recentlyAssignedNumber = TestData.Number("N");
        var recentlyCompletedNumber = TestData.Number("N");
        var oldStatusChangeNumber = TestData.Number("N");
        var draftNumber = TestData.Number("N");

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
                Number = recentlyAssignedNumber,
                Title = "Recent Status Change",
                Creator = creator,
                Assignee = otherUser,
                Status = WorkOrderStatus.Assigned,
                DueDate = futureDate,
                AssignedDate = now.AddDays(-2)
            };

            var recentlyCompleted = new WorkOrder
            {
                Number = recentlyCompletedNumber,
                Title = "Recently Completed",
                Creator = creator,
                Assignee = otherUser,
                Status = WorkOrderStatus.Complete,
                DueDate = futureDate,
                CompletedDate = now.AddDays(-1)
            };

            var oldStatusChange = new WorkOrder
            {
                Number = oldStatusChangeNumber,
                Title = "Old Status Change",
                Creator = creator,
                Assignee = otherUser,
                Status = WorkOrderStatus.InProgress,
                DueDate = futureDate,
                AssignedDate = now.AddDays(-10)
            };

            var draft = new WorkOrder
            {
                Number = draftNumber,
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
        numbers.ShouldContain(recentlyAssignedNumber);
        numbers.ShouldContain(recentlyCompletedNumber);
    }
}
