using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Computes SLA health (on-time completion percentage) for the current month and last month.
/// A work order counts toward SLA if it is completed (Status == Complete) and has a DueDate
/// within that month. SLA = (on-time count / total count) * 100, where on-time means CompletedDate &lt;= DueDate.
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

        var currentMonthData = await ComputeMonthSla(currentMonthStart, currentMonthEnd, cancellationToken);
        var lastMonthData = await ComputeMonthSla(lastMonthStart, lastMonthEnd, cancellationToken);

        return new SlaHealth
        {
            CurrentMonthPercentage = currentMonthData.Percentage,
            LastMonthPercentage = lastMonthData.Percentage,
            CurrentMonthOrderCount = currentMonthData.TotalCount,
            LastMonthOrderCount = lastMonthData.TotalCount
        };
    }

    private async Task<MonthSlaData> ComputeMonthSla(DateOnly monthStart, DateOnly monthEnd, CancellationToken cancellationToken)
    {
        var orders = await context.Set<WorkOrder>()
            .AsNoTracking()
            .Where(wo => wo.Status == WorkOrderStatus.Complete && wo.DueDate != null && wo.CompletedDate.HasValue)
            .ToListAsync(cancellationToken);

        var monthOrders = orders
            .Where(wo => IsInMonth(wo.CompletedDate!.Value, monthStart, monthEnd))
            .ToList();

        if (monthOrders.Count == 0)
        {
            return new MonthSlaData { TotalCount = 0, Percentage = null };
        }

        var onTimeCount = monthOrders.Count(wo =>
            DateOnly.FromDateTime(wo.CompletedDate!.Value) <= wo.DueDate!.Value);
        var percentage = (decimal)onTimeCount / monthOrders.Count * 100;

        return new MonthSlaData { TotalCount = monthOrders.Count, Percentage = Math.Round(percentage, 0) };
    }

    private static bool IsInMonth(DateTime completedDate, DateOnly monthStart, DateOnly monthEnd)
    {
        var completedDateOnly = DateOnly.FromDateTime(completedDate);
        return completedDateOnly >= monthStart && completedDateOnly <= monthEnd;
    }

    private sealed class MonthSlaData
    {
        public decimal? Percentage { get; init; }
        public int TotalCount { get; init; }
    }
}
