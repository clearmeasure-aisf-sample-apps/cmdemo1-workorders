using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Computes SLA health (on-time completion percentage) for the current month and last month.
/// Only completions whose church date falls in those two months are read, as due date and
/// completed date. SLA = (on-time count / total count) * 100, where on-time means the church
/// completion date is on or before the due date. The Native AOT builds use <see cref="AotSlaHealthQueryHandler"/> instead.
/// </summary>
public class SlaHealthQueryHandler(DataContext context, TimeProvider time) : IRequestHandler<SlaHealthQuery, SlaHealth>
{
    public async ValueTask<SlaHealth> Handle(SlaHealthQuery request, CancellationToken cancellationToken = default)
    {
        var currentMonth = ChurchMonth.Containing(ChurchTimeZone.Today(time));
        var orders = await CompletedWithDueDateAsync(currentMonth.Previous(), currentMonth, cancellationToken);
        return SlaHealth.ForMonths(orders, currentMonth);
    }

    /// <summary>
    /// Completed work orders with a due date whose church completion date falls in the two months, projected to the
    /// due and completed dates so creator and assignee are not joined.
    /// </summary>
    private async Task<List<WorkOrder>> CompletedWithDueDateAsync(
        ChurchMonth firstMonth,
        ChurchMonth lastMonth,
        CancellationToken cancellationToken)
    {
        var (windowStart, windowEnd) = SlaHealth.CompletionWindow(firstMonth.FirstDay, lastMonth.LastDay);
        var complete = WorkOrderStatus.Complete;
        var ct = cancellationToken;
        var rows = await context.Set<WorkOrder>()
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Where(wo => wo.Status == complete
                && wo.DueDate != null
                && wo.CompletedDate >= windowStart
                && wo.CompletedDate < windowEnd)
            .Select(wo => new { wo.DueDate, wo.CompletedDate })
            .ToListAsync(ct);

        return rows.Select(row => new WorkOrder
        {
            DueDate = row.DueDate,
            CompletedDate = row.CompletedDate,
            Status = WorkOrderStatus.Complete
        }).ToList();
    }
}
