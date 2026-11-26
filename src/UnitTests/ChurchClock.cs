using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests;

/// <summary>
/// Test mother for frozen <see cref="TimeProvider"/> clocks used by church calendar tests, built on
/// <see cref="FixedUtcClock"/>.
/// </summary>
public static class ChurchClock
{
    /// <summary>A clock frozen at 12:00 <see cref="ChurchTimeZone.Chicago"/> local time on the given date.</summary>
    public static TimeProvider NoonUtcOn(int year, int month, int day)
    {
        var local = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(local, ChurchTimeZone.Chicago);
        return AtUtc(new DateTimeOffset(utc, TimeSpan.Zero));
    }

    /// <summary>A clock frozen at <paramref name="utcNow"/>, which must carry a zero UTC offset.</summary>
    /// <exception cref="ArgumentException">When <paramref name="utcNow"/> has a non-zero offset.</exception>
    public static TimeProvider AtUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException($"Expected a UTC instant but the offset was {utcNow.Offset}.", nameof(utcNow));
        }

        return FixedUtcClock.At(utcNow);
    }
}
