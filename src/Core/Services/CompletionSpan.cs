namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// One completed work order reduced to what the days-to-close math needs. The span is derived once, at construction,
/// from the stored creation and completion instants; it is null when either instant is missing.
/// </summary>
public readonly record struct CompletionSpan
{
    /// <summary>
    /// Creates a span from the work order's stored creation and completion instants.
    /// </summary>
    public CompletionSpan(DateTime? createdDate, DateTime? completedDate)
    {
        DaysToClose = createdDate.HasValue && completedDate.HasValue
            ? (completedDate.Value - createdDate.Value).TotalDays
            : null;
    }

    /// <summary>
    /// Days from creation to completion, or null when the creation or completion instant is missing.
    /// </summary>
    public double? DaysToClose { get; }
}
