using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Lists the distinct, non-blank buildings used by work orders, trimmed and sorted, for the
/// Building filter.
/// </summary>
public class WorkOrderBuildingsQueryHandler(DataContext context) : IRequestHandler<WorkOrderBuildingsQuery, string[]>
{
    public async Task<string[]> Handle(WorkOrderBuildingsQuery request, CancellationToken cancellationToken = default)
    {
        var buildings = await context.Set<WorkOrder>().AsNoTracking()
            .Where(wo => wo.Building != null)
            .Select(wo => wo.Building!)
            .Distinct()
            .ToListAsync(cancellationToken);

        return buildings
            .Select(b => b.Trim())
            .Where(b => b.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(b => b, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
