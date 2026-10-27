using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderStatusHistoryQueryHandler(DbContext dbContext) : IRequestHandler<WorkOrderStatusHistoryQuery, WorkOrderStatusChange[]>
{
    public async Task<WorkOrderStatusChange[]> Handle(WorkOrderStatusHistoryQuery query, CancellationToken cancellationToken)
    {
        var history = await dbContext.Set<WorkOrderStatusChange>()
            .Where(c => c.WorkOrderId == query.WorkOrderId)
            .OrderByDescending(c => c.ChangedDate)
            .Include(c => c.ChangedBy)
            .ToArrayAsync(cancellationToken);

        return history;
    }
}
