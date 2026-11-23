namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// A calendar month of church (America/Chicago) dates, from its first to its last day.
/// </summary>
public readonly record struct ChurchMonth(DateOnly FirstDay, DateOnly LastDay)
{
    public static ChurchMonth Containing(DateOnly day)
    {
        var firstDay = new DateOnly(day.Year, day.Month, 1);
        return new ChurchMonth(firstDay, firstDay.AddMonths(1).AddDays(-1));
    }

    public ChurchMonth Previous() => Containing(FirstDay.AddDays(-1));

    public bool Contains(DateOnly day) => day >= FirstDay && day <= LastDay;

    /// <summary>
    /// Stored completion instants whose church calendar date falls in
    /// <paramref name="sinceMonth"/> through this month, inclusive.
    /// Used to query work orders completed across one or more church months
    /// for SLA health reporting. Both ends are <see cref="DateTimeKind.Unspecified"/>.
    /// </summary>
    public (DateTime StartInclusive, DateTime EndExclusive) CompletionWindow(ChurchMonth sinceMonth)
    {
        if (sinceMonth.FirstDay > LastDay)
        {
            throw new ArgumentException(
                "The since month must be the same as or earlier than the receiver month.",
                nameof(sinceMonth));
        }

        return (StoredInstant(sinceMonth.FirstDay), StoredInstant(LastDay.AddDays(1)));
    }

    private static DateTime StoredInstant(DateOnly churchDate)
    {
        var localMidnight = churchDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(localMidnight, ChurchTimeZone.Chicago);
        return DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);
    }
}
