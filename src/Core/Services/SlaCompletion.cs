namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// The due date and stored completion instant of one completed work order, the only inputs the SLA math needs.
/// The church-calendar completion date is derived once, here, from the stored instant.
/// </summary>
public readonly record struct SlaCompletion(DateOnly DueDate, DateTime CompletedDate)
{
    private readonly DateOnly _completed = ChurchTimeZone.ChurchDate(CompletedDate);

    /// <summary>
    /// The church-calendar date (America/Chicago) of the stored <see cref="CompletedDate"/> instant.
    /// </summary>
    public DateOnly Completed => _completed;

    /// <summary>
    /// True when <see cref="Completed"/> is on or before <see cref="DueDate"/>.
    /// </summary>
    public bool IsOnTime => _completed <= DueDate;
}
