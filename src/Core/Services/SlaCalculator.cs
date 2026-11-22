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

        var diff = currentPercent - lastPercent;
        return diff > 0
            ? $"↑ up from {lastPercent:0}%"
            : diff < 0
                ? $"↓ down from {lastPercent:0}%"
                : $"— flat at {lastPercent:0}%";
    }

    /// <summary>
    /// Computes the on-time percentage (0–100, rounded to a whole number) for the given orders.
    /// Orders without a DueDate or CompletedDate are excluded. On time means the church-local completion date is on or before the DueDate.
    /// Returns a null percentage when no qualifying orders exist.
    /// </summary>
    public static (decimal? Percentage, int TotalCount) CalculateOnTimePercentage(IEnumerable<WorkOrder> orders)
    {
        var qualifying = orders.Where(wo => wo is { DueDate: not null, CompletedDate: not null }).ToList();
        if (qualifying.Count == 0)
        {
            return (null, 0);
        }

        var onTimeCount = qualifying.Count(wo =>
            ChurchTimeZone.ChurchDate(wo.CompletedDate!.Value) <= wo.DueDate!.Value);
        var percentage = (decimal)onTimeCount / qualifying.Count * 100;
        return (Math.Round(percentage, 0), qualifying.Count);
    }
}
