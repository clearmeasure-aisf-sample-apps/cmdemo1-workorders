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
        var today = ChurchTimeZone.Today(time);
        var currentMonthStart = new DateOnly(today.Year, today.Month, 1);
        var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
        var lastMonthStart = currentMonthStart.AddMonths(-1);
        var lastMonthEnd = currentMonthStart.AddDays(-1);
        var (windowStart, windowEnd) = SlaHealth.CompletionWindow(lastMonthStart, currentMonthEnd);

        // Completion dates in the two church months only, projected so creator and assignee are not joined.
        var rows = await context.Set<WorkOrder>()
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Where(wo => wo.Status == WorkOrderStatus.Complete
                && wo.DueDate != null
                && wo.CompletedDate >= windowStart
                && wo.CompletedDate < windowEnd)
            .Select(wo => new { wo.DueDate, wo.CompletedDate })
            .ToListAsync(cancellationToken);

        var orders = rows.Select(row => new WorkOrder
        {
            DueDate = row.DueDate,
            CompletedDate = row.CompletedDate,
            Status = WorkOrderStatus.Complete
        }).ToList();

        // Compute both months in memory
        var currentMonthData = ComputeMonthSlaInMemory(orders, currentMonthStart, currentMonthEnd);
        var lastMonthData = ComputeMonthSlaInMemory(orders, lastMonthStart, lastMonthEnd);

        return new SlaHealth
        {
            CurrentMonthPercentage = currentMonthData.Percentage,
            LastMonthPercentage = lastMonthData.Percentage,
            CurrentMonthOrderCount = currentMonthData.TotalCount,
            LastMonthOrderCount = lastMonthData.TotalCount
        };
    }

    private static MonthSlaData ComputeMonthSlaInMemory(List<WorkOrder> orders, DateOnly monthStart, DateOnly monthEnd)
    {
        var monthOrders = orders
            .Where(wo => IsInMonth(wo.CompletedDate!.Value, monthStart, monthEnd))
            .ToList();

        var (percentage, totalCount) = SlaHealth.CalculateOnTimePercentage(monthOrders);
        return new MonthSlaData { TotalCount = totalCount, Percentage = percentage };
    }

    private static bool IsInMonth(DateTime completedDate, DateOnly monthStart, DateOnly monthEnd)
    {
        var completedDateOnly = SlaHealth.ChurchDate(completedDate);
        return completedDateOnly >= monthStart && completedDateOnly <= monthEnd;
    }

    private sealed class MonthSlaData
    {
        public decimal? Percentage { get; init; }
        public int TotalCount { get; init; }
    }
}
