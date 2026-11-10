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
        var currentMonthData = ComputeMonthSla(orders, currentMonth);
        var lastMonthData = ComputeMonthSla(orders, lastMonth);

        return new SlaHealth
        {
            CurrentMonthPercentage = currentMonthData.Percentage,
            LastMonthPercentage = lastMonthData.Percentage,
            CurrentMonthOrderCount = currentMonthData.TotalCount,
            LastMonthOrderCount = lastMonthData.TotalCount
        };
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

    private static MonthSlaData ComputeMonthSla(List<WorkOrder> orders, ChurchMonth month)
    {
        var monthOrders = orders
            .Where(wo => month.Contains(SlaHealth.ChurchDate(wo.CompletedDate!.Value)))
            .ToList();

        var (percentage, totalCount) = SlaHealth.CalculateOnTimePercentage(monthOrders);
        return new MonthSlaData { TotalCount = totalCount, Percentage = percentage };
    }

    private sealed class MonthSlaData
    {
        public decimal? Percentage { get; init; }
        public int TotalCount { get; init; }
    }
}
