using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Counts work orders per status with the same status filter Work Order Search applies, so each
/// count equals the search total for that status. Statuses are grouped in one query; a status
/// with no rows stays at zero.
/// </summary>
public class WorkOrderCountByStatusQueryHandler(DataContext context) :
    IRequestHandler<WorkOrderCountByStatusQuery, Dictionary<string, int>>
{
    public async Task<Dictionary<string, int>> Handle(WorkOrderCountByStatusQuery request,
        CancellationToken cancellationToken = default)
    {
        var grouped = await context.Set<WorkOrder>()
            .AsNoTracking()
            .GroupBy(workOrder => workOrder.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<string, int>();
        foreach (var status in WorkOrderStatus.GetAllItems())
        {
            result[status.Key] = 0;
        }

        foreach (var row in grouped)
        {
            result[row.Status.Key] = row.Count;
        }

        return result;
    }
}
