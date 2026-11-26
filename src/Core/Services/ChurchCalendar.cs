namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Instance service for the church's local calendar (America/Chicago), bound to one clock.
/// Delegates to <see cref="ChurchTimeZone"/> so both give the same answers.
/// </summary>
public class ChurchCalendar
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates a church calendar that reads the current instant from <paramref name="timeProvider"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// The host has no America/Chicago or Central Standard Time zone data, so
    /// <see cref="ChurchTimeZone.Chicago"/> fell back to UTC (<see cref="ChurchTimeZone.IsFallback"/>).
    /// </exception>
    public ChurchCalendar(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (ChurchTimeZone.IsFallback)
        {
            throw new InvalidOperationException(
                "America/Chicago time zone data is missing on this host; church dates would be computed in UTC.");
        }

        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Today's calendar date in America/Chicago (see <see cref="ChurchTimeZone.Today"/>).
    /// </summary>
    public DateOnly Today => ChurchTimeZone.Today(_timeProvider);

    /// <summary>
    /// The coming Saturday in America/Chicago, today when today is Saturday
    /// (see <see cref="ChurchTimeZone.ComingSaturday"/>).
    /// </summary>
    public DateOnly ComingSaturday => ChurchTimeZone.ComingSaturday(_timeProvider);
}
