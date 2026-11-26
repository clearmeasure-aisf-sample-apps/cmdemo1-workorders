using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// The SLA math (on-time percentage and month-over-month trend) that belongs to no single entity.
/// </summary>
public static class SlaCalculator
{
    /// <summary>
    /// Compares this month's percentage with last month's. Answers <see cref="SlaTrend.Up"/>, <see cref="SlaTrend.Down"/>
    /// or <see cref="SlaTrend.Flat"/> with last month's percentage, or <see cref="SlaTrend.Unknown"/> (no percentage)
    /// when either month has no data. Display text is the UI's concern.
    /// </summary>
    public static SlaTrendComparison Trend(decimal? currentPercent, decimal? lastPercent)
    {
        if (lastPercent is null || currentPercent is null)
        {
            return new SlaTrendComparison(SlaTrend.Unknown, null);
        }

        var trend = currentPercent.Value.CompareTo(lastPercent.Value) switch
        {
            > 0 => SlaTrend.Up,
            < 0 => SlaTrend.Down,
            _ => SlaTrend.Flat
        };
        return new SlaTrendComparison(trend, lastPercent);
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
