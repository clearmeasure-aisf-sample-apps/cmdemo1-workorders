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
}
