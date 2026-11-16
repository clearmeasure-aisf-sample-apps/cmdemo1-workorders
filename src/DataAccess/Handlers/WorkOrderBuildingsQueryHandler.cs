using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Lists the distinct, non-blank buildings used by work orders, trimmed and sorted, for the
/// Building filter.
/// </summary>
public class WorkOrderBuildingsQueryHandler(DataContext context) : IRequestHandler<WorkOrderBuildingsQuery, string[]>
{
    // A field, not the primary-constructor parameter, so EF 11 can precompile the queries for Native AOT.
    // ReSharper disable once ReplaceWithPrimaryConstructorParameter
    private readonly DataContext _context = context;

    public async ValueTask<string[]> Handle(WorkOrderBuildingsQuery request, CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;
        var buildings = await _context.Set<WorkOrder>().AsNoTracking()
            .Where(wo => wo.Building != null)
            .Select(wo => wo.Building!)
            .Distinct()
            .ToListAsync(ct);

        return buildings
            .Select(b => b.Trim())
            .Where(b => b.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(b => b, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
