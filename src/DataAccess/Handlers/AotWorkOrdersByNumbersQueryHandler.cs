using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// <see cref="WorkOrdersByNumbersQueryHandler"/> for the UI.Server Native AOT build. EF 11 cannot precompile Contains
/// over a parameter list, and the run-time fallback needs converter code AOT removed, so the numbers are matched over
/// the <see cref="WorkOrderSnapshot"/>, ignoring case like the SQL Server collation.
/// </summary>
public class AotWorkOrdersByNumbersQueryHandler(DataContext context, TimeProvider time)
    : IRequestHandler<WorkOrdersByNumbersQuery, WorkOrder[]>
{
    private readonly WorkOrderSnapshotSource _source = new(context, time);

    public async ValueTask<WorkOrder[]> Handle(WorkOrdersByNumbersQuery request,
        CancellationToken cancellationToken = default)
    {
        var numbers = request.DistinctNumbers().ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (numbers.Count == 0)
        {
            return [];
        }

        var snapshot = await _source.LoadAsync(cancellationToken);
        return snapshot.All.Where(workOrder => workOrder.Number != null && numbers.Contains(workOrder.Number)).ToArray();
    }
}
