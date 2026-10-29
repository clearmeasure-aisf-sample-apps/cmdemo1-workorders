using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Counts work orders per status with the same status filter Work Order Search applies, so each
/// count equals the search total for that status.
/// </summary>
public class WorkOrderCountByStatusQueryHandler(DataContext context) :
    IRequestHandler<WorkOrderCountByStatusQuery, Dictionary<string, int>>
{
    public async Task<Dictionary<string, int>> Handle(WorkOrderCountByStatusQuery request,
        CancellationToken cancellationToken = default)
    {
        var orders = context.Set<WorkOrder>().AsNoTracking();
        var result = new Dictionary<string, int>();
        foreach (var status in WorkOrderStatus.GetAllItems())
        {
            result[status.Key] = await WorkOrderQueryFilters.Apply(orders, null, null, status)
                .CountAsync(cancellationToken);
        }

        return result;
    }
}
