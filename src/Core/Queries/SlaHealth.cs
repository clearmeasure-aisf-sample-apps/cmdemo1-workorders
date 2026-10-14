// ReSharper disable PropertyCanBeMadeInitOnly.Global -- System.Text.Json round-trip serialization (remoting protocol) requires mutable setters
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
}
