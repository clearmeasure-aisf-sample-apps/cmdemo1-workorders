using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderStatusHistoryQueryHandler(DbContext dbContext) : IRequestHandler<WorkOrderStatusHistoryQuery, WorkOrderStatusChange[]>
{
    private readonly DataContext dbContext = (DataContext)dbContext;

    public async ValueTask<WorkOrderStatusChange[]> Handle(WorkOrderStatusHistoryQuery query, CancellationToken cancellationToken)
    {
        var workOrderId = query.WorkOrderId;
        var ct = cancellationToken;
        var history = await dbContext.Set<WorkOrderStatusChange>()
            .Where(c => c.WorkOrderId == workOrderId)
            .OrderByDescending(c => c.ChangedDate)
            .Include(c => c.ChangedBy)
            .ToArrayAsync(ct);

        return history;
    }
}
