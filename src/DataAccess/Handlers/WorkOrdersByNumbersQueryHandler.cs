using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>Reads the work orders a bulk action targets, in one query.</summary>
public class WorkOrdersByNumbersQueryHandler(DataContext context) : IRequestHandler<WorkOrdersByNumbersQuery, WorkOrder[]>
{
    // A field, not the primary-constructor parameter, so EF 11 can precompile the queries for Native AOT.
    // ReSharper disable once ReplaceWithPrimaryConstructorParameter
    private readonly DataContext _context = context;

    public async ValueTask<WorkOrder[]> Handle(WorkOrdersByNumbersQuery request,
        CancellationToken cancellationToken = default)
    {
        var numbers = request.DistinctNumbers();
        if (numbers.Length == 0)
        {
            return [];
        }

        var ct = cancellationToken;
        return await _context.Set<WorkOrder>()
            .AsNoTracking()
            .Where(workOrder => workOrder.Number != null && numbers.Contains(workOrder.Number))
            .ToArrayAsync(ct);
    }
}
