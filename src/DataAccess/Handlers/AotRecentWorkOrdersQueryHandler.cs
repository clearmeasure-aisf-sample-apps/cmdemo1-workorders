using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;
/// <summary>
/// <see cref="RecentWorkOrdersQueryHandler"/> for the UI.Server Native AOT build: the same newest-activity order, read
/// from the <see cref="WorkOrderSnapshot"/>.
/// </summary>
public class AotRecentWorkOrdersQueryHandler(DataContext context, TimeProvider time)
    : IRequestHandler<RecentWorkOrdersQuery, WorkOrder[]>
{
    private readonly WorkOrderSnapshotSource _source = new(context, time);

    public async ValueTask<WorkOrder[]> Handle(RecentWorkOrdersQuery request, CancellationToken cancellationToken = default)
    {
        var snapshot = await _source.LoadAsync(cancellationToken);
        return snapshot.All
            .Where(wo => WorkOrderActivity.Latest(wo) != null)
            .OrderByDescending(wo => WorkOrderActivity.Latest(wo)!.WhenUtc)
            .ThenBy(wo => wo.Number, StringComparer.Ordinal)
            .Take(request.Count)
            .ToArray();
    }
}
