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

        var overdue = mine
            .Where(wo => WorkOrderSnapshot.IsOpen(wo) && wo.DueDate <= snapshot.Today)
            .OrderByDescending(wo => wo.DueDate).ThenByDescending(wo => wo.CreatedDate)
            .ToArray();
        var newlyAssigned = mine
            .Where(wo => wo.AssignedDate >= sevenDaysAgo)
            .OrderByDescending(wo => wo.AssignedDate)
            .ToArray();
        var createdByMeChanged = snapshot.CreatedBy(employee.Id)
            .Where(wo => (wo.AssignedDate >= sevenDaysAgo && wo.Status != WorkOrderStatus.Draft) ||
                         (wo.CompletedDate >= sevenDaysAgo && wo.Status == WorkOrderStatus.Complete))
            .OrderByDescending(wo => wo.AssignedDate ?? wo.CompletedDate)
            .ToArray();

        return new NotificationsSummary(overdue, newlyAssigned, createdByMeChanged);
    }
}
