namespace ClearMeasure.Bootcamp.UnitTests;

/// <summary>
/// Fixed UTC clock for unit tests that need a deterministic <see cref="TimeProvider"/>.
/// </summary>
public sealed class FixedUtcClock(DateTimeOffset utcNow) : TimeProvider
{
    /// <summary>
    /// Returns <see cref="TimeZoneInfo.Utc"/> so <see cref="TimeProvider.GetLocalNow"/>
    /// is deterministic regardless of the host machine's zone.
    /// </summary>
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => utcNow;

    /// <summary>Creates a clock frozen at <paramref name="utcNow"/>.</summary>
    public static TimeProvider At(DateTimeOffset utcNow) => new FixedUtcClock(utcNow);
}
