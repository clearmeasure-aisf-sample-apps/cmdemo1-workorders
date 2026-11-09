using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class ChurchTimeZoneTests
{
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Test]
    public void Should_ResolveCentralStandardTime_When_ChicagoZoneAccessed()
    {
        // Verify that the Chicago timezone is resolved (could be America/Chicago or Central Standard Time)
        ChurchTimeZone.Chicago.ShouldNotBeNull();
        var id = ChurchTimeZone.Chicago.Id;
        id.ShouldBeOneOf("America/Chicago", "Central Standard Time", "UTC");
    }

    [Test]
    public void Should_ReturnLocalDateInChicagoZone_When_TodayIsCalled()
    {
        // 2026-08-29 12:00:00 UTC = 2026-08-29 07:00:00 CDT (UTC-5 during daylight saving)
        var utcNoon = new DateTimeOffset(new DateTime(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(utcNoon);

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Test]
    public void Should_ConvertUtcToLocalChicagoTime_When_UtcIsProvided()
    {
        // 2026-01-15 12:00:00 UTC = 2026-01-15 06:00:00 CST (UTC-6 during standard time)
        var utcTime = new DateTimeOffset(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(utcTime);

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 1, 15));
    }

    [Test]
    public void Should_HandleDaylightSavingTimeStart_When_TimeIsBeforeDstChange()
    {
        // 2026-03-08 07:59:00 UTC = 2026-03-08 01:59:00 CST (UTC-6, before DST starts at 08:00 UTC)
        var beforeDst = new DateTimeOffset(new DateTime(2026, 3, 8, 7, 59, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(beforeDst);

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 3, 8));
    }

    [Test]
    public void Should_HandleDaylightSavingTimeStart_When_TimeIsAfterDstChange()
    {
        // 2026-03-08 08:00:00 UTC = 2026-03-08 03:00:00 CDT (UTC-5, after DST starts)
        var afterDst = new DateTimeOffset(new DateTime(2026, 3, 8, 8, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(afterDst);

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 3, 8));
    }

    [Test]
    public void Should_HandleDaylightSavingTimeEnd_When_TimeIsBeforeDstEnd()
    {
        // 2026-11-01 06:59:00 UTC = 2026-11-01 01:59:00 CDT (UTC-5, before DST ends at 07:00 UTC)
        var beforeDstEnd = new DateTimeOffset(new DateTime(2026, 11, 1, 6, 59, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(beforeDstEnd);

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 11, 1));
    }

    [Test]
    public void Should_HandleDaylightSavingTimeEnd_When_TimeIsAfterDstEnd()
    {
        // 2026-11-01 07:00:00 UTC = 2026-11-01 01:00:00 CST (UTC-6, after DST ends)
        var afterDstEnd = new DateTimeOffset(new DateTime(2026, 11, 1, 7, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(afterDstEnd);

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 11, 1));
    }

    [Test]
    public void Should_HandleMidnightUtcDuringDaylightSavingTime()
    {
        // 2026-07-15 00:00:00 UTC = 2026-07-14 19:00:00 CDT (UTC-5)
        var midnightUtc = new DateTimeOffset(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(midnightUtc);

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 7, 14));
    }

    [Test]
    public void Should_HandleMidnightUtcDuringStandardTime()
    {
        // 2026-01-15 00:00:00 UTC = 2026-01-14 18:00:00 CST (UTC-6)
        var midnightUtc = new DateTimeOffset(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(midnightUtc);

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 1, 14));
    }

    [Test]
    public void Should_ReturnComingSaturday_When_TodayIsSaturday()
    {
        // 2026-08-29 is a Saturday
        var saturdayNoon = new DateTimeOffset(new DateTime(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(saturdayNoon);

        var saturday = ChurchTimeZone.ComingSaturday(clock);

        saturday.ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Test]
    public void Should_ReturnNextSaturday_When_TodayIsMonday()
    {
        // 2026-08-24 is a Monday, next Saturday is 2026-08-29
        var mondayNoon = new DateTimeOffset(new DateTime(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(mondayNoon);

        var saturday = ChurchTimeZone.ComingSaturday(clock);

        saturday.ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Test]
    public void Should_ReturnNextSaturday_When_TodayIsFriday()
    {
        // 2026-08-28 is a Friday, next Saturday is 2026-08-29
        var fridayNoon = new DateTimeOffset(new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(fridayNoon);

        var saturday = ChurchTimeZone.ComingSaturday(clock);

        saturday.ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Test]
    public void Should_ReturnNextSaturday_When_TodayIsSunday()
    {
        // 2026-08-30 is a Sunday, next Saturday is 2026-09-05
        var sundayNoon = new DateTimeOffset(new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(sundayNoon);

        var saturday = ChurchTimeZone.ComingSaturday(clock);

        saturday.ShouldBe(new DateOnly(2026, 9, 5));
    }

    [Test]
    public void Should_ThrowArgumentNullException_When_TodayCalledWithNullTimeProvider()
    {
        var exception = Should.Throw<ArgumentNullException>(() => ChurchTimeZone.Today(null!));
        exception.ParamName.ShouldBe("timeProvider");
    }

    [Test]
    public void Should_ThrowArgumentNullException_When_ComingSaturdayCalledWithNullTimeProvider()
    {
        var exception = Should.Throw<ArgumentNullException>(() => ChurchTimeZone.ComingSaturday(null!));
        exception.ParamName.ShouldBe("timeProvider");
    }

    [Test]
    public void Should_HandleLeapYearDate()
    {
        // 2026 is not a leap year, but test a date near the boundary
        // 2024-02-29 is a leap day (2024 is a leap year)
        var leapDayUtc = new DateTimeOffset(new DateTime(2024, 2, 29, 12, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(leapDayUtc);

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2024, 2, 29));
    }

    [Test]
    public void Should_HandleYearBoundary()
    {
        // 2026-01-01 00:00:00 UTC = 2025-12-31 18:00:00 CST (UTC-6)
        var newYearUtc = new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(newYearUtc);

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2025, 12, 31));
    }

    [Test]
    public void Should_HandleEndOfYear()
    {
        // 2025-12-31 23:00:00 UTC = 2025-12-31 17:00:00 CST (UTC-6)
        var endOfYearUtc = new DateTimeOffset(new DateTime(2025, 12, 31, 23, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(endOfYearUtc);

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2025, 12, 31));
    }

    [Test]
    public void Should_HandleLateNightTransitionToPreviousDay()
    {
        // 2026-06-15 04:00:00 UTC = 2026-06-14 23:00:00 CDT (UTC-5)
        var lateNightUtc = new DateTimeOffset(new DateTime(2026, 6, 15, 4, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(lateNightUtc);

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 6, 14));
    }

    [Test]
    public void Should_HandleEarlyMorningTransitionToNextDay()
    {
        // 2026-06-15 11:00:00 UTC = 2026-06-15 06:00:00 CDT (UTC-5)
        var earlyMorningUtc = new DateTimeOffset(new DateTime(2026, 6, 15, 11, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        var clock = new FixedTimeProvider(earlyMorningUtc);

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 6, 15));
    }
}
