using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Computes the facility's work order figures for the Reports page.
/// All open counts exclude Complete and Cancelled statuses; completed counts use the same
/// 30-day window and averaging logic as the search page's summary KPI.
/// </summary>
public class WorkOrderReportsQueryHandler(DataContext context, TimeProvider time)
    : IRequestHandler<WorkOrderReportsQuery, WorkOrderReportsData>
{
    private const int CompletedWindowDays = 30;
    private const int WeeksToShow = 8;

    public async Task<WorkOrderReportsData> Handle(WorkOrderReportsQuery request,
        CancellationToken cancellationToken = default)
    {
        var orders = context.Set<WorkOrder>().AsNoTracking();
        var today = ChurchTimeZone.Today(time);
        var open = WorkOrderQueryFilters.OpenOnly(orders);

        var data = new WorkOrderReportsData
        {
            OverdueCount = await WorkOrderQueryFilters.Overdue(orders, today).CountAsync(cancellationToken)
        };

        await AddCountByStatusAsync(data, open, cancellationToken);
        await AddCountByPriorityAsync(data, open, cancellationToken);
        await AddCountByBuildingAsync(data, open, cancellationToken);
        await AddCompletedPerWeekAsync(data, orders, today, cancellationToken);
        await AddAverageDaysToCloseAsync(data, orders, cancellationToken);

        return data;
    }

    private static async Task AddCountByStatusAsync(WorkOrderReportsData data, IQueryable<WorkOrder> openOrders,
        CancellationToken cancellationToken)
    {
        var statuses = await openOrders
            .Select(wo => wo.Status)
            .ToListAsync(cancellationToken);

        // Group in memory because Status uses a value converter
        var statusGroups = statuses
            .GroupBy(s => s.Key)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var (key, count) in statusGroups)
        {
            data.CountByStatus[key] = count;
        }
    }

    private static async Task AddCountByPriorityAsync(WorkOrderReportsData data, IQueryable<WorkOrder> openOrders,
        CancellationToken cancellationToken)
    {
        var priorities = await openOrders
            .Select(wo => wo.Priority)
            .ToListAsync(cancellationToken);

        // Group in memory because Priority is an enum value converter
        var priorityGroups = priorities
            .GroupBy(p => p.ToString())
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var (key, count) in priorityGroups)
        {
            data.CountByPriority[key] = count;
        }
    }

    private static async Task AddCountByBuildingAsync(WorkOrderReportsData data, IQueryable<WorkOrder> openOrders,
        CancellationToken cancellationToken)
    {
        var buildings = await openOrders
            .Where(wo => wo.Building != null)
            .Select(wo => wo.Building)
            .ToListAsync(cancellationToken);

        var buildingGroups = buildings
            .GroupBy(b => b!)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var (building, count) in buildingGroups)
        {
            data.CountByBuilding[building] = count;
        }
    }

    private async Task AddCompletedPerWeekAsync(WorkOrderReportsData data, IQueryable<WorkOrder> orders,
        DateOnly today, CancellationToken cancellationToken)
    {
        var startDate = today.AddDays(-(WeeksToShow * 7));
        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
        var completions = await orders
            .Where(wo => wo.Status == WorkOrderStatus.Complete && wo.CompletedDate >= startDateTime)
            .Select(wo => new { wo.CompletedDate })
            .ToListAsync(cancellationToken);

        // Generate all weeks in the range (some may have zero count)
        var weeklyData = new Dictionary<DateOnly, int>();
        for (int i = WeeksToShow - 1; i >= 0; i--)
        {
            var weekEnd = GetWeekEndingDate(today.AddDays(-i * 7));
            weeklyData[weekEnd] = 0;
        }

        // Count completions by week
        foreach (var completion in completions)
        {
            if (completion.CompletedDate.HasValue)
            {
                var weekEnd = GetWeekEndingDate(DateOnly.FromDateTime(completion.CompletedDate.Value));
                if (weeklyData.ContainsKey(weekEnd))
                {
                    weeklyData[weekEnd]++;
                }
            }
        }

        data.CompletedPerWeek = weeklyData
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new WorkOrderReportsData.WeeklyCount { WeekEndingDate = kvp.Key, Count = kvp.Value })
            .ToList();
    }

    private async Task AddAverageDaysToCloseAsync(WorkOrderReportsData data, IQueryable<WorkOrder> orders,
        CancellationToken cancellationToken)
    {
        var completions = await WorkOrderQueryFilters.CompletedWithin(orders, CompletedWindowDays, time)
            .Select(wo => new { wo.CreatedDate, wo.CompletedDate })
            .ToListAsync(cancellationToken);

        var durations = completions
            .Where(c => c.CreatedDate.HasValue && c.CompletedDate.HasValue)
            .Select(c => (c.CompletedDate!.Value - c.CreatedDate!.Value).TotalDays)
            .ToList();

        data.CompletedLast30Days = completions.Count;
        data.AverageDaysToClose = durations.Count == 0 ? null : durations.Average();
    }

    /// <summary>
    /// Returns the week-ending date (Sunday) for a given date.
    /// The week-ending Sunday is the most recent Sunday on or before the given date.
    /// </summary>
    private static DateOnly GetWeekEndingDate(DateOnly date)
    {
        // Calculate days since the most recent Sunday
        var daysSinceSunday = (int)date.DayOfWeek;
        if (daysSinceSunday == 0)
        {
            // If it's already Sunday, that's the week-ending date
            return date;
        }

        // Return the most recent Sunday (backwards from date)
        return date.AddDays(-daysSinceSunday);
    }
}
