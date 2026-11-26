using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

/// <summary>
/// Pins <see cref="ChurchTimeZone.Today"/>: the America/Chicago calendar date of the clock's current instant.
/// </summary>
[TestFixture]
public class ChurchTimeZoneTodayTests
{
    [Test]
    public void Should_ReturnLocalDateInChicagoZone_When_TodayIsCalled()
    {
        // 2026-08-29 12:00:00 UTC = 2026-08-29 07:00:00 CDT (UTC-5 during daylight saving)
        var clock = StubUtcClock.At(new DateTime(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc));

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Test]
    public void Should_ConvertUtcToLocalChicagoTime_When_UtcIsProvided()
    {
        // 2026-01-15 12:00:00 UTC = 2026-01-15 06:00:00 CST (UTC-6 during standard time)
        var clock = StubUtcClock.At(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc));

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 1, 15));
    }

    [Test]
    public void Should_HandleDaylightSavingTimeStart_When_TimeIsBeforeDstChange()
    {
        // 2026-03-08 07:59:00 UTC = 2026-03-08 01:59:00 CST (UTC-6, before DST starts at 08:00 UTC)
        var clock = StubUtcClock.At(new DateTime(2026, 3, 8, 7, 59, 0, DateTimeKind.Utc));

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 3, 8));
    }

    [Test]
    public void Should_HandleDaylightSavingTimeStart_When_TimeIsAfterDstChange()
    {
        // 2026-03-08 08:00:00 UTC = 2026-03-08 03:00:00 CDT (UTC-5, after DST starts)
        var clock = StubUtcClock.At(new DateTime(2026, 3, 8, 8, 0, 0, DateTimeKind.Utc));

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 3, 8));
    }

    [Test]
    public void Should_HandleDaylightSavingTimeEnd_When_TimeIsBeforeDstEnd()
    {
        // 2026-11-01 06:59:00 UTC = 2026-11-01 01:59:00 CDT (UTC-5, before DST ends at 07:00 UTC)
        var clock = StubUtcClock.At(new DateTime(2026, 11, 1, 6, 59, 0, DateTimeKind.Utc));

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 11, 1));
    }

    [Test]
    public void Should_HandleDaylightSavingTimeEnd_When_TimeIsAfterDstEnd()
    {
        // 2026-11-01 07:00:00 UTC = 2026-11-01 01:00:00 CST (UTC-6, after DST ends)
        var clock = StubUtcClock.At(new DateTime(2026, 11, 1, 7, 0, 0, DateTimeKind.Utc));

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 11, 1));
    }

    [Test]
    public void Should_HandleMidnightUtcDuringDaylightSavingTime()
    {
        // 2026-07-15 00:00:00 UTC = 2026-07-14 19:00:00 CDT (UTC-5)
        var clock = StubUtcClock.At(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc));

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 7, 14));
    }

    [Test]
    public void Should_HandleMidnightUtcDuringStandardTime()
    {
        // 2026-01-15 00:00:00 UTC = 2026-01-14 18:00:00 CST (UTC-6)
        var clock = StubUtcClock.At(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 1, 14));
    }

    [Test]
    public void Should_ThrowArgumentNullException_When_TodayCalledWithNullTimeProvider()
    {
        var exception = Should.Throw<ArgumentNullException>(() => ChurchTimeZone.Today(null!));

        exception.ParamName.ShouldBe("timeProvider");
    }

    [Test]
    public void Should_HandleLeapYearDate()
    {
        // 2024-02-29 is a leap day (2024 is a leap year)
        var clock = StubUtcClock.At(new DateTime(2024, 2, 29, 12, 0, 0, DateTimeKind.Utc));

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2024, 2, 29));
    }

    [Test]
    public void Should_HandleYearBoundary()
    {
        // 2026-01-01 00:00:00 UTC = 2025-12-31 18:00:00 CST (UTC-6)
        var clock = StubUtcClock.At(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2025, 12, 31));
    }

    [Test]
    public void Should_HandleEndOfYear()
    {
        // 2025-12-31 23:00:00 UTC = 2025-12-31 17:00:00 CST (UTC-6)
        var clock = StubUtcClock.At(new DateTime(2025, 12, 31, 23, 0, 0, DateTimeKind.Utc));

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2025, 12, 31));
    }

    [Test]
    public void Should_HandleLateNightTransitionToPreviousDay()
    {
        // 2026-06-15 04:00:00 UTC = 2026-06-14 23:00:00 CDT (UTC-5)
        var clock = StubUtcClock.At(new DateTime(2026, 6, 15, 4, 0, 0, DateTimeKind.Utc));

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 6, 14));
    }

    [Test]
    public void Should_HandleEarlyMorningTransitionToNextDay()
    {
        // 2026-06-15 11:00:00 UTC = 2026-06-15 06:00:00 CDT (UTC-5)
        var clock = StubUtcClock.At(new DateTime(2026, 6, 15, 11, 0, 0, DateTimeKind.Utc));

        var today = ChurchTimeZone.Today(clock);

        today.ShouldBe(new DateOnly(2026, 6, 15));
    }
}
