using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderSnapshotNotificationsTests
{
    private static readonly DateTime UtcNow = new(2026, 10, 13, 18, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 10, 13);

    private static readonly Employee Me = EmployeeBuilder.Create("notifyme").WithId(Guid.NewGuid()).Build();
    private static readonly Employee Other = EmployeeBuilder.Create("notifyother").WithId(Guid.NewGuid()).Build();

    [Test]
    public void ShouldListMyOpenOverdueOrDueToday_NewestDueFirst()
    {
        var dueToday = Mine(WorkOrderStatus.Assigned).DueOn(Today).Build();
        var overdue = Mine(WorkOrderStatus.InProgress).DueOn(Today.AddDays(-3)).Build();
        var doneOverdue = Mine(WorkOrderStatus.Complete).DueOn(Today.AddDays(-3)).Build();
        var dueLater = Mine(WorkOrderStatus.Assigned).DueOn(Today.AddDays(2)).Build();

        var panel = WorkOrderSnapshotNotifications.For(
            WorkOrderSnapshotTests.Snapshot(overdue, dueToday, doneOverdue, dueLater), Me.Id);

        panel.OverdueAndDueToday.ShouldBe([dueToday, overdue]);
    }

    [Test]
    public void ShouldListMyAssignmentsFromTheLastSevenDays_NewestFirst()
    {
        var yesterday = Mine(WorkOrderStatus.Assigned).AssignedAt(UtcNow.AddDays(-1)).Build();
        var today = Mine(WorkOrderStatus.Assigned).AssignedAt(UtcNow.AddHours(-1)).Build();
        var lastMonth = Mine(WorkOrderStatus.Assigned).AssignedAt(UtcNow.AddDays(-30)).Build();

        var panel = WorkOrderSnapshotNotifications.For(
            WorkOrderSnapshotTests.Snapshot(yesterday, lastMonth, today), Me.Id);

        panel.NewlyAssignedLast7Days.ShouldBe([today, yesterday]);
    }

    [Test]
    public void ShouldListWorkOrdersICreatedThatWereAssignedOrCompletedThisWeek()
    {
        var assigned = Created(WorkOrderStatus.Assigned).AssignedAt(UtcNow.AddDays(-2)).Build();
        var completed = Created(WorkOrderStatus.Complete).CompletedAt(UtcNow.AddDays(-1)).Build();
        var draftWithOldAssignDate = Created(WorkOrderStatus.Draft).AssignedAt(UtcNow.AddDays(-1)).Build();
        var assignedLongAgo = Created(WorkOrderStatus.Assigned).AssignedAt(UtcNow.AddDays(-20)).Build();

        var panel = WorkOrderSnapshotNotifications.For(
            WorkOrderSnapshotTests.Snapshot(completed, assigned, draftWithOldAssignDate, assignedLongAgo), Me.Id);

        panel.CreatedByMeStatusChangedLast7Days.ShouldBe([completed, assigned]);
    }

    private static WorkOrderBuilder Mine(WorkOrderStatus status) =>
        WorkOrderBuilder.For(Other, status).AssignedTo(Me);

    private static WorkOrderBuilder Created(WorkOrderStatus status) =>
        WorkOrderBuilder.For(Me, status).AssignedTo(Other);
}
