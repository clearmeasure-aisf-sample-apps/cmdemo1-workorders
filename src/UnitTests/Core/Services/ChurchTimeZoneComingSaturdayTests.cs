using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

/// <summary>
/// Pins <see cref="ChurchTimeZone.ComingSaturday"/>: the next Saturday in America/Chicago, today when today is
/// Saturday. The one fixture for this member.
/// </summary>
[TestFixture]
public class ChurchTimeZoneComingSaturdayTests
{
    [Test]
    public void Should_ReturnComingSaturday_When_TodayIsSaturday()
    {
        // 2026-08-29 is a Saturday
        var clock = StubUtcClock.At(new DateTime(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc));

        var saturday = ChurchTimeZone.ComingSaturday(clock);

        saturday.ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Test]
    public void Should_ReturnNextSaturday_When_TodayIsMonday()
    {
        // 2026-08-24 is a Monday, next Saturday is 2026-08-29
        var clock = StubUtcClock.At(new DateTime(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc));

        var saturday = ChurchTimeZone.ComingSaturday(clock);

        saturday.ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Test]
    public void Should_ReturnNextSaturday_When_TodayIsFriday()
    {
        // 2026-08-28 is a Friday, next Saturday is 2026-08-29
        var clock = StubUtcClock.At(new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc));

        var saturday = ChurchTimeZone.ComingSaturday(clock);

        saturday.ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Test]
    public void Should_ReturnNextSaturday_When_TodayIsSunday()
    {
        // 2026-08-30 is a Sunday, next Saturday is 2026-09-05
        var clock = StubUtcClock.At(new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc));

        var saturday = ChurchTimeZone.ComingSaturday(clock);

        saturday.ShouldBe(new DateOnly(2026, 9, 5));
    }

    [Test]
    public void Should_ReturnToday_When_ChicagoLocalNoonIsSaturday()
    {
        // 2026-08-29 is a Saturday; the clock reads 12:00 America/Chicago local time
        var clock = ChurchClock.NoonUtcOn(2026, 8, 29);

        var saturday = ChurchTimeZone.ComingSaturday(clock);

        saturday.ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Test]
    public void Should_ReturnNextDay_When_ChicagoLocalNoonIsFriday()
    {
        // 2026-08-28 is a Friday; the clock reads 12:00 America/Chicago local time
        var clock = ChurchClock.NoonUtcOn(2026, 8, 28);

        var saturday = ChurchTimeZone.ComingSaturday(clock);

        saturday.ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Test]
    public void Should_ThrowArgumentNullException_When_ComingSaturdayCalledWithNullTimeProvider()
    {
        var exception = Should.Throw<ArgumentNullException>(() => ChurchTimeZone.ComingSaturday(null!));

        exception.ParamName.ShouldBe("timeProvider");
    }
}
