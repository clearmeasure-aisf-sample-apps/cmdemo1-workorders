using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// The SLA math (on-time percentage and trend text) that belongs to no single entity.
/// </summary>
public static class SlaCalculator
{
    /// <summary>
    /// Computes the trend text and aria-label relative to the last month. Returns "↑ up from X%", "↓ down from X%",
    /// or "— flat at X%", or null if the last month has no data (no comparison possible).
    /// </summary>
    public static string? TrendText(decimal? currentPercent, decimal? lastPercent)
    {
        if (lastPercent is null || currentPercent is null)
        {
            return null;
        }

        var diff = currentPercent.Value - lastPercent.Value;
        return diff.CompareTo(0) switch
        {
            > 0 => $"↑ up from {lastPercent:0}%",
            < 0 => $"↓ down from {lastPercent:0}%",
            _ => $"— flat at {lastPercent:0}%"
        };
    }

    /// <summary>
    /// Computes the on-time percentage (0–100, rounded to a whole number) for the given orders.
    /// Orders without a DueDate or CompletedDate are excluded. On time means the church-local completion date is on or
    /// before the DueDate. Midpoints use banker's rounding (<see cref="MidpointRounding.ToEven"/>): 5 of 8 on time
    /// (62.5) answers 62 and 3 of 8 (37.5) answers 38.
    /// Returns a null percentage when no qualifying orders exist.
    /// </summary>
    public static (decimal? Percentage, int TotalCount) CalculateOnTimePercentage(IEnumerable<WorkOrder> orders)
    {
        var qualifying = QualifyingDates(orders).ToList();
        if (qualifying.Count == 0)
        {
            return (null, 0);
        }

        var onTimeCount = qualifying.Count(dates => dates.Completed <= dates.Due);
        var percentage = (decimal)onTimeCount / qualifying.Count * 100;
        return (Math.Round(percentage, 0, MidpointRounding.ToEven), qualifying.Count);
    }

    /// <summary>
    /// The church completion date and the due date of each order that has both.
    /// </summary>
    private static IEnumerable<(DateOnly Completed, DateOnly Due)> QualifyingDates(IEnumerable<WorkOrder> orders)
    {
        foreach (var order in orders)
        {
            if (order is { DueDate: { } due, CompletedDate: { } completed })
            {
                yield return (ChurchTimeZone.ChurchDate(completed), due);
            }
        }
    }
}
