using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// <see cref="SlaHealthQueryHandler"/> for the UI.Server Native AOT build: the same on-time percentages, from the completed
/// work orders in the <see cref="WorkOrderSnapshot"/>. EF 11's precompiled form of the JIT handler's filtered query fails
/// to bind its parameters (NullReferenceException in TypeMappedRelationalParameter.AddDbParameter), so it answered 500.
/// </summary>
public class AotSlaHealthQueryHandler(DataContext context, TimeProvider time) : IRequestHandler<SlaHealthQuery, SlaHealth>
{
    private readonly WorkOrderSnapshotSource _source = new(context, time);

    public async ValueTask<SlaHealth> Handle(SlaHealthQuery request, CancellationToken cancellationToken = default)
    {
        var snapshot = await _source.LoadAsync(cancellationToken);
        var completed = snapshot.All.Where(wo => wo.Status == WorkOrderStatus.Complete);
        return SlaHealth.ForMonths(completed, ChurchMonth.Containing(snapshot.Today));
    }
}
