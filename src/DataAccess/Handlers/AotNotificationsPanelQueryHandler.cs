using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;
/// <summary>
/// <see cref="NotificationsPanelQueryHandler"/> for the UI.Server Native AOT build, read from the
/// <see cref="WorkOrderSnapshot"/> with the same filters and orders.
/// </summary>
public class AotNotificationsPanelQueryHandler(DataContext context, TimeProvider time)
    : IRequestHandler<NotificationsPanelQuery, NotificationsSummary>
{
    private readonly WorkOrderSnapshotSource _source = new(context, time);

    public async ValueTask<NotificationsSummary> Handle(NotificationsPanelQuery request,
        CancellationToken cancellationToken = default)
    {
        var employee = string.IsNullOrWhiteSpace(request.UserName)
            ? null
            : await _source.FindEmployeeAsync(request.UserName, cancellationToken);
        if (employee == null)
        {
            return new NotificationsSummary([], [], []);
        }

        var snapshot = await _source.LoadAsync(cancellationToken);
        var sevenDaysAgo = snapshot.UtcNow.AddDays(-7);
        var mine = snapshot.AssignedTo(employee.Id).ToList();

        return new NotificationsSummary(
            OverdueOrDueToday(mine, snapshot.Today),
            AssignedSince(mine, sevenDaysAgo),
            ChangedSince(snapshot.CreatedBy(employee.Id), sevenDaysAgo));
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
