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
    /// Computes the on-time percentage (0–100, rounded to a whole number) for the given orders, after mapping each
    /// order that has both a DueDate and a CompletedDate to <see cref="SlaCompletion"/> and delegating to
    /// <see cref="CalculateOnTimePercentage(IEnumerable{SlaCompletion})"/>.
    /// </summary>
    public static (decimal? Percentage, int TotalCount) CalculateOnTimePercentage(IEnumerable<WorkOrder> orders)
    {
        var completions = new List<SlaCompletion>();
        foreach (var order in orders)
        {
            if (order is { DueDate: { } due, CompletedDate: { } completed })
            {
                completions.Add(new SlaCompletion(due, completed));
            }
        }

        return CalculateOnTimePercentage(completions);
    }

    /// <summary>
    /// Computes the on-time percentage (0–100, rounded to a whole number) from the due dates and stored completion
    /// instants of completed work orders, without the work orders themselves. On time means the church-local
    /// completion date is on or before the DueDate. Midpoints use banker's rounding (<see cref="MidpointRounding.ToEven"/>):
    /// 5 of 8 on time (62.5) answers 62 and 3 of 8 (37.5) answers 38.
    /// Returns (null, 0) when no completions are given.
    /// </summary>
    public static (decimal? Percentage, int TotalCount) CalculateOnTimePercentage(IEnumerable<SlaCompletion> completions)
    {
        var completionsList = completions.ToList();
        if (completionsList.Count == 0)
        {
            return (null, 0);
        }

        var onTimeCount = completionsList.Count(c => c.IsOnTime);
        var percentage = (decimal)onTimeCount / completionsList.Count * 100;
        return (Math.Round(percentage, 0, MidpointRounding.ToEven), completionsList.Count);
    }
}
