using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderQueryHandler(DataContext context) :
    IRequestHandler<WorkOrderByNumberQuery, WorkOrder?>
{
    // A field, not the primary-constructor parameter, so EF 11 can precompile the queries for Native AOT.
    // ReSharper disable once ReplaceWithPrimaryConstructorParameter
    private readonly DataContext _context = context;

    public async Task<WorkOrder?> GetWorkOrderAsync(string number)
    {
        var workOrderNumber = number;
        return await _context.Set<WorkOrder>()
            .AsNoTracking()
            .SingleOrDefaultAsync(wo => wo.Number == workOrderNumber);
    }

    public async Task<WorkOrder[]> GetWorkOrdersAsync(WorkOrderSearchSpecification specification)
    {
        var assignee = specification.Assignee;
        var creator = specification.Creator;
        var status = specification.Status;
        return await _context.Set<WorkOrder>()
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
}
