using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

/// <summary>
/// A <see cref="TimeProvider"/> frozen at one UTC instant, for church calendar tests.
/// </summary>
internal sealed class StubUtcClock(DateTimeOffset utcNow) : TimeProvider
{
    /// <summary>
    /// Returns the frozen instant.
    /// </summary>
    public override DateTimeOffset GetUtcNow() => utcNow;

    /// <summary>
    /// A clock frozen at the given UTC wall-clock time.
    /// </summary>
    public static StubUtcClock At(DateTime utc) => new(new DateTimeOffset(utc, TimeSpan.Zero));

    /// <summary>
    /// A clock frozen at 12:00 America/Chicago local time on the given date.
    /// </summary>
    public static StubUtcClock ChicagoNoonOn(int year, int month, int day)
    {
        var local = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Unspecified);
        return At(TimeZoneInfo.ConvertTimeToUtc(local, ChurchTimeZone.Chicago));
    }
}
