using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;
/// <summary>
/// <see cref="NotificationsPanelQueryHandler"/> for the UI.Server Native AOT build, read with
/// <see cref="WorkOrderSnapshotNotifications"/> from the <see cref="WorkOrderSnapshot"/>.
/// </summary>
public class AotNotificationsPanelQueryHandler(DataContext context, TimeProvider time)
    : IRequestHandler<NotificationsPanelQuery, NotificationsSummary>
{
    private readonly WorkOrderSnapshotSource _source = new(context, time);

    public async ValueTask<NotificationsSummary> Handle(NotificationsPanelQuery request,
        CancellationToken cancellationToken = default)
    {
        var employee = await _source.FindEmployeeAsync(request.UserName, cancellationToken);
        if (employee == null)
        {
            return new NotificationsSummary([], [], []);
        }

        var snapshot = await _source.LoadAsync(cancellationToken);
        return WorkOrderSnapshotNotifications.For(snapshot, employee.Id);
    }
}
