// ReSharper disable PropertyCanBeMadeInitOnly.Global -- System.Text.Json round-trip serialization (remoting protocol) requires mutable setters
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// SLA health metrics for a month: the percentage of work orders completed on time (CompletedDate &lt;= DueDate).
/// Null percentage when no qualifying orders exist (all orders either have no DueDate or aren't completed).
/// </summary>
public class SlaHealth
{
    /// <summary>Percentage of work orders completed on time this month (0–100), or null when no qualifying orders exist.</summary>
    public decimal? CurrentMonthPercentage { get; set; }

    /// <summary>Percentage of work orders completed on time last month (0–100), or null when no qualifying orders exist.</summary>
    public decimal? LastMonthPercentage { get; set; }

    /// <summary>Total number of completed work orders in the current month that have a DueDate.</summary>
    public int CurrentMonthOrderCount { get; set; }

    /// <summary>Total number of completed work orders in the last month that have a DueDate.</summary>
    public int LastMonthOrderCount { get; set; }

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
            ChurchDate(wo.CompletedDate!.Value) <= wo.DueDate!.Value);
        var percentage = (decimal)onTimeCount / qualifying.Count * 100;
        return (Math.Round(percentage, 0), qualifying.Count);
    }

    /// <summary>
    /// Gets the church calendar date (America/Chicago) of a completion time. Stored values are UTC and come back with
    /// <see cref="DateTimeKind.Unspecified"/>, so the value is treated as UTC.
    /// </summary>
    public static DateOnly ChurchDate(DateTime completedUtc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(completedUtc, DateTimeKind.Utc), ChurchTimeZone.Chicago));
}
