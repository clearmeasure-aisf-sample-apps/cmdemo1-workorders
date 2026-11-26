using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

/// <summary>
/// Pins <see cref="ChurchTimeZone.ChurchDate"/>: the America/Chicago calendar date of a stored UTC instant.
/// </summary>
[TestFixture]
public class ChurchTimeZoneChurchDateTests
{
    [Test]
    public void Should_ReturnPreviousChurchDate_When_ChurchDateIsOneSecondBeforeChicagoMidnight()
    {
        // 2026-01-15 05:59:59 UTC = 2026-01-14 23:59:59 CST (UTC-6 during standard time)
        var storedUtc = new DateTime(2026, 1, 15, 5, 59, 59);

        var date = ChurchTimeZone.ChurchDate(storedUtc);

        date.ShouldBe(new DateOnly(2026, 1, 14));
    }

    [Test]
    public void Should_ReturnSameChurchDate_When_ChurchDateIsExactlyChicagoMidnightInDaylightTime()
    {
        // 2026-07-15 05:00:00 UTC = 2026-07-15 00:00:00 CDT (UTC-5 during daylight saving)
        var storedUtc = new DateTime(2026, 7, 15, 5, 0, 0);

        var date = ChurchTimeZone.ChurchDate(storedUtc);

        date.ShouldBe(new DateOnly(2026, 7, 15));
    }

    [Test]
    public void Should_ReturnSameChurchDate_When_ChurchDateInstantIsUtcKindInsteadOfUnspecified()
    {
        // The same instant declared with DateTimeKind.Utc must map to the same date as Unspecified
        var unspecified = ChurchTimeZone.ChurchDate(new DateTime(2026, 1, 15, 6, 0, 0));

        var utc = ChurchTimeZone.ChurchDate(new DateTime(2026, 1, 15, 6, 0, 0, DateTimeKind.Utc));

        utc.ShouldBe(unspecified);
        utc.ShouldBe(new DateOnly(2026, 1, 15));
    }

    [Test]
    public void Should_ReturnPreviousChurchDate_When_ChurchDatePrecedesDstStart()
    {
        // 2026-03-08 05:30:00 UTC is 2026-03-07 23:30:00 CST (UTC-6): DST starts at 08:00 UTC.
        // Reading the offset as UTC-5 would wrongly answer March 8.
        var storedUtc = new DateTime(2026, 3, 8, 5, 30, 0);

        var date = ChurchTimeZone.ChurchDate(storedUtc);

        date.ShouldBe(new DateOnly(2026, 3, 7));
    }

    [Test]
    public void Should_ReturnSameChurchDate_When_ChurchDatePrecedesDstEnd()
    {
        // 2026-11-01 05:30:00 UTC is 2026-11-01 00:30:00 CDT (UTC-5): DST ends at 07:00 UTC.
        // Reading the offset as UTC-6 would wrongly answer October 31.
        var storedUtc = new DateTime(2026, 11, 1, 5, 30, 0);

        var date = ChurchTimeZone.ChurchDate(storedUtc);

        date.ShouldBe(new DateOnly(2026, 11, 1));
    }

    [Test]
    public void Should_ReturnPreviousYearDate_When_ChurchDateIsNewYearUtcBeforeChicagoMidnight()
    {
        // 2026-01-01 05:30:00 UTC = 2025-12-31 23:30:00 CST (UTC-6 during standard time)
        var storedUtc = new DateTime(2026, 1, 1, 5, 30, 0);

        var date = ChurchTimeZone.ChurchDate(storedUtc);

        date.ShouldBe(new DateOnly(2025, 12, 31));
    }
}
