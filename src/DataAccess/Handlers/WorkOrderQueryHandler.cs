using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderQueryHandler(DataContext context) :
    IRequestHandler<WorkOrderByNumberQuery, WorkOrder?>,
    IRequestHandler<WorkOrdersByNumbersQuery, WorkOrder[]>
{
    private readonly DataContext context = context;

    public async Task<WorkOrder?> GetWorkOrderAsync(string number)
    {
        var workOrderNumber = number;
        return await context.Set<WorkOrder>()
            .AsNoTracking()
            .SingleOrDefaultAsync(wo => wo.Number == workOrderNumber);
    }

    public async Task<WorkOrder[]> GetWorkOrdersAsync(WorkOrderSearchSpecification specification)
    {
        var assignee = specification.Assignee;
        var creator = specification.Creator;
        var status = specification.Status;
        return await context.Set<WorkOrder>()
            .Where(wo => (assignee == null || wo.Assignee == assignee)
                && (creator == null || wo.Creator == creator)
                && (status == null || wo.Status == status))
            .ToArrayAsync();
    }

    public async ValueTask<WorkOrder?> Handle(WorkOrderByNumberQuery request,
        CancellationToken cancellationToken = default)
    {
        return await GetWorkOrderAsync(request.Number);
    }

    /// <summary>Loads every requested work order in one query. Blanks and duplicates count once.</summary>
    public async ValueTask<WorkOrder[]> Handle(WorkOrdersByNumbersQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request.Numbers is not { Length: > 0 })
        {
            return [];
        }

        var ct = cancellationToken;
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
            .ToArrayAsync(ct);
    }
}