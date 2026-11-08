using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderQueryHandler(DataContext context) :
    IRequestHandler<WorkOrderByNumberQuery, WorkOrder?>,
    IRequestHandler<WorkOrdersByNumbersQuery, WorkOrder[]>
{
    public async Task<WorkOrder?> GetWorkOrderAsync(string number)
    {
        return await context.Set<WorkOrder>()
            .AsNoTracking()
            .SingleOrDefaultAsync(wo => wo.Number == number);
    }

    public async Task<WorkOrder[]> GetWorkOrdersAsync(WorkOrderSearchSpecification specification)
    {
        var query = WorkOrderQueryFilters.Apply(context.Set<WorkOrder>(), specification);
        return await query.ToArrayAsync();
    }

    public async Task<WorkOrder?> Handle(WorkOrderByNumberQuery request,
        CancellationToken cancellationToken = default)
    {
        return await GetWorkOrderAsync(request.Number);
    }

    /// <summary>Loads every requested work order in one query. Blanks and duplicates count once.</summary>
    public async Task<WorkOrder[]> Handle(WorkOrdersByNumbersQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request.Numbers is not { Length: > 0 })
        {
            return [];
        }

        var numbers = request.Numbers
            .Where(number => !string.IsNullOrWhiteSpace(number))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (numbers.Count == 0)
        {
            return [];
        }

        if (numbers.Count > WorkOrdersByNumbersQuery.MaxNumbers)
        {
            throw new InvalidOperationException(
                $"WorkOrdersByNumbersQuery accepts at most {WorkOrdersByNumbersQuery.MaxNumbers} numbers.");
        }

        return await context.Set<WorkOrder>()
            .AsNoTracking()
            .Where(workOrder => workOrder.Number != null && numbers.Contains(workOrder.Number))
            .ToArrayAsync(cancellationToken);
    }
}