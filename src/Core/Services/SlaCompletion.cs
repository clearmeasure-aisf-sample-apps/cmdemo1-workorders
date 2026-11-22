namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// The due date and church-calendar completion date of one completed work order, the only inputs the SLA math needs.
/// The church-calendar completion date is derived once, at construction, from the stored completion instant; the
/// instant itself is not kept because nothing reads it after that.
/// </summary>
public readonly record struct SlaCompletion
{
    /// <summary>
    /// Creates a completion from the work order's due date and its stored completion instant.
    /// </summary>
    public SlaCompletion(DateOnly dueDate, DateTime completedDate)
    {
        DueDate = dueDate;
        Completed = ChurchTimeZone.ChurchDate(completedDate);
    }

    /// <summary>
    /// The church-calendar date the work order was due.
    /// </summary>
    public DateOnly DueDate { get; }

    /// <summary>
    /// The church-calendar date (America/Chicago) of the stored completion instant.
    /// </summary>
    public DateOnly Completed { get; }

    /// <summary>
    /// True when <see cref="Completed"/> is on or before <see cref="DueDate"/>.
    /// </summary>
    public bool IsOnTime => Completed <= DueDate;
}
