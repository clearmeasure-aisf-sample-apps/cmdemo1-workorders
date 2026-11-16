using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Runs a <see cref="WorkOrderSpecificationQuery"/> in the Native AOT builds (<c>McpServerAot=true</c> or
/// <c>UiServerAot=true</c>), in place of <c>WorkOrderSearchHandler</c>, with <see cref="WorkOrderSnapshotSearch"/>
/// over the <see cref="WorkOrderSnapshot"/>.
/// </summary>
/// <remarks>
/// EF 11's query precompiler cannot take <c>WorkOrderSearchHandler</c>'s composed query. Written as one fixed query, it
/// evaluates an optional filter such as <c>assignee == null || wo.Assignee == assignee</c> at build time and silently
/// drops it. So this handler filters and orders the snapshot in memory.
/// </remarks>
public class AotWorkOrderSearchHandler(DataContext context, TimeProvider time)
    : IRequestHandler<WorkOrderSpecificationQuery, WorkOrder[]>
{
    private readonly WorkOrderSnapshotSource _source = new(context, time);

    public async ValueTask<WorkOrder[]> Handle(WorkOrderSpecificationQuery specification,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _source.LoadAsync(cancellationToken);
        return WorkOrderSnapshotSearch.Run(snapshot, specification);
    }
}
