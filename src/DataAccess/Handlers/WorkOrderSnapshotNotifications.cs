using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// The notifications panel for one employee, read from a <see cref="WorkOrderSnapshot"/> with the same filters and
/// orders as <c>NotificationsPanelQueryHandler</c>. The UI.Server Native AOT build answers the panel this way
/// (<c>AotNotificationsPanelQueryHandler</c>); the JIT build compiles it too so unit tests pin its filters.
/// </summary>
internal static class WorkOrderSnapshotNotifications
{
    public static NotificationsSummary For(WorkOrderSnapshot snapshot, Guid employeeId)
    {
        var sevenDaysAgo = snapshot.UtcNow.AddDays(-7);
        var mine = snapshot.AssignedTo(employeeId).ToList();

        return new NotificationsSummary(
            OverdueOrDueToday(mine, snapshot.Today),
            AssignedSince(mine, sevenDaysAgo),
            ChangedSince(snapshot.CreatedBy(employeeId), sevenDaysAgo));
    }

    private static WorkOrder[] OverdueOrDueToday(IEnumerable<WorkOrder> mine, DateOnly today) =>
        mine.Where(wo => WorkOrderSnapshot.IsOpen(wo) && wo.DueDate <= today)
            .OrderByDescending(wo => wo.DueDate).ThenByDescending(wo => wo.CreatedDate)
            .ToArray();

    private static WorkOrder[] AssignedSince(IEnumerable<WorkOrder> mine, DateTime since) =>
        mine.Where(wo => wo.AssignedDate >= since)
            .OrderByDescending(wo => wo.AssignedDate)
            .ToArray();

    /// <summary>Work orders I created that someone has since assigned or completed.</summary>
    private static WorkOrder[] ChangedSince(IEnumerable<WorkOrder> createdByMe, DateTime since) =>
        createdByMe
            .Where(wo => (wo.AssignedDate >= since && wo.Status != WorkOrderStatus.Draft) ||
                         (wo.CompletedDate >= since && wo.Status == WorkOrderStatus.Complete))
            .OrderByDescending(wo => wo.AssignedDate ?? wo.CompletedDate)
            .ToArray();
}
