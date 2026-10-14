using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Runs a <see cref="WorkOrderSpecificationQuery"/>; results are newest first, then by number.
/// </summary>
public class WorkOrderSearchHandler(DataContext context, TimeProvider time) : IRequestHandler<WorkOrderSpecificationQuery, WorkOrder[]>
{
    public async Task<WorkOrder[]> Handle(WorkOrderSpecificationQuery specification,
        CancellationToken cancellationToken = default)
    {
        var query = WorkOrderQueryFilters.Apply(context.Set<WorkOrder>(), specification, time)
            .OrderByDescending(wo => wo.CreatedDate)
            .ThenBy(wo => wo.Number);
        return await query.ToArrayAsync(cancellationToken);
    }
}
