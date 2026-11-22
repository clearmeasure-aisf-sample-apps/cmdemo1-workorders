using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Computes SLA health (on-time completion percentage) for the current month and last month.
/// Only completions whose church date falls in those two months are read, as due date and
/// completed date. SLA = (on-time count / total count) * 100, where on-time means the church
/// completion date is on or before the due date.
/// </summary>
public class SlaHealthQueryHandler(DataContext context, TimeProvider time) : IRequestHandler<SlaHealthQuery, SlaHealth>
{
    public async Task<SlaHealth> Handle(SlaHealthQuery request, CancellationToken cancellationToken = default)
    {
        var currentMonth = ChurchMonth.Containing(ChurchTimeZone.Today(time));
        var lastMonth = currentMonth.Previous();
        var orders = await CompletedWithDueDateAsync(lastMonth, currentMonth, cancellationToken);
        var (currentPercentage, currentCount) = SlaCalculator.CalculateOnTimePercentage(CompletedIn(orders, currentMonth));
        var (lastPercentage, lastCount) = SlaCalculator.CalculateOnTimePercentage(CompletedIn(orders, lastMonth));

        return new SlaHealth
        {
            CurrentMonthPercentage = currentPercentage,
            LastMonthPercentage = lastPercentage,
            CurrentMonthOrderCount = currentCount,
            LastMonthOrderCount = lastCount
        };
    }

    /// <summary>
    /// Completed work orders with a due date whose church completion date falls in <paramref name="sinceMonth"/> through
    /// <paramref name="throughMonth"/>, projected to the due and completed dates so creator and assignee are not joined.
    /// </summary>
    private async Task<List<WorkOrder>> CompletedWithDueDateAsync(
        ChurchMonth sinceMonth,
        ChurchMonth throughMonth,
        CancellationToken cancellationToken)
    {
        var (windowStart, windowEnd) = ChurchTimeZone.CompletionWindow(sinceMonth.FirstDay, throughMonth.LastDay);
        var rows = await context.Set<WorkOrder>()
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Where(wo => wo.Status == WorkOrderStatus.Complete
                && wo.DueDate != null
                && wo.CompletedDate >= windowStart
                && wo.CompletedDate < windowEnd)
            .Select(wo => new { wo.DueDate, wo.CompletedDate })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new WorkOrder
        {
            DueDate = row.DueDate,
            CompletedDate = row.CompletedDate,
            Status = WorkOrderStatus.Complete
        }).ToList();
    }

    private static IEnumerable<WorkOrder> CompletedIn(IEnumerable<WorkOrder> orders, ChurchMonth month) =>
        orders.Where(wo => wo.CompletedDate is { } completed && month.Contains(ChurchTimeZone.ChurchDate(completed)));
}
