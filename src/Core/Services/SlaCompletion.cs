namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// One completed work order reduced to what the SLA math needs: the church-calendar completion date and whether it
/// was on time. Both are derived once, at construction, from the due date and the stored completion instant; the raw
/// inputs are not kept because nothing reads them after that.
/// </summary>
public readonly record struct SlaCompletion
{
    /// <summary>
    /// Creates a completion from the work order's due date and its stored completion instant.
    /// </summary>
    public SlaCompletion(DateOnly dueDate, DateTime completedDate)
    {
        Completed = ChurchTimeZone.ChurchDate(completedDate);
        IsOnTime = Completed <= dueDate;
    }

    /// <summary>
    /// The church-calendar date (America/Chicago) of the stored completion instant.
    /// </summary>
    public DateOnly Completed { get; }

    /// <summary>
    /// True when <see cref="Completed"/> is on or before the due date.
    /// </summary>
    public bool IsOnTime { get; }
}
