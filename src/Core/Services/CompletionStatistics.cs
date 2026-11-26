namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// The completion figures shown by the Reports page and the Work Order Search summary: how many work orders
/// completed in a window and their average days to close.
/// </summary>
public readonly record struct CompletionStatistics
{
    /// <summary>
    /// Computes the figures from the completions in the window. Every completion counts; only those with a
    /// <see cref="CompletionSpan.DaysToClose"/> feed the average.
    /// </summary>
    public CompletionStatistics(IReadOnlyCollection<CompletionSpan> completions)
    {
        var durations = completions
            .Where(c => c.DaysToClose.HasValue)
            .Select(c => c.DaysToClose!.Value)
            .ToList();

        Count = completions.Count;
        AverageDaysToClose = durations.Count == 0 ? null : durations.Average();
    }

    /// <summary>
    /// Number of completions in the window.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Average days from creation to completion, or null when no completion has both instants.
    /// </summary>
    public double? AverageDaysToClose { get; }
}
