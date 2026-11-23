namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Resolves the church's local calendar timezone (America/Chicago) with a Windows fallback.
/// </summary>
public static class ChurchTimeZone
{
    private const string IanaId = "America/Chicago";
    private const string WindowsId = "Central Standard Time";

    /// <summary>
    /// Gets America/Chicago, then Central Standard Time, with UTC as a safe final fallback.
    /// </summary>
    public static TimeZoneInfo Chicago { get; } = ResolveChicago();

    /// <summary>
    /// Returns today's calendar date in America/Chicago for the given clock, through
    /// <see cref="ChurchDate"/>, the one place that converts a stored instant.
    /// </summary>
    public static DateOnly Today(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return ChurchDate(timeProvider.GetUtcNow().UtcDateTime);
    }

    /// <summary>
    /// Returns the coming Saturday in America/Chicago (today when today is Saturday).
    /// </summary>
    public static DateOnly ComingSaturday(TimeProvider timeProvider)
    {
        var today = Today(timeProvider);
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(daysUntilSaturday);
    }

    /// <summary>
    /// Returns the church calendar date (America/Chicago) of a stored instant. Persisted values are
    /// UTC but come back as <see cref="DateTimeKind.Unspecified"/>, so the value is read as UTC.
    /// </summary>
    public static DateOnly ChurchDate(DateTime storedUtc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(storedUtc, DateTimeKind.Utc), Chicago));

    /// <summary>
    /// Stored completion instants whose church calendar date falls in
    /// <paramref name="monthStart"/> through <paramref name="monthEnd"/>, inclusive.
    /// Both ends are <see cref="DateTimeKind.Unspecified"/>, matching persisted
    /// <c>CompletedDate</c> values, which <see cref="ChurchTimeZone.ChurchDate"/> reads as UTC.
    /// </summary>
    public static (DateTime StartInclusive, DateTime EndExclusive) CompletionWindow(DateOnly monthStart, DateOnly monthEnd)
    {
        return (StoredInstant(monthStart), StoredInstant(monthEnd.AddDays(1)));
    }

    private static DateTime StoredInstant(DateOnly churchDate)
    {
        var localMidnight = churchDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(localMidnight, Chicago);
        return DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);
    }

    private static TimeZoneInfo ResolveChicago() =>
        FindTimeZone(IanaId)
        ?? FindTimeZone(WindowsId)
        ?? TimeZoneInfo.Utc;

    private static TimeZoneInfo? FindTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }
}
