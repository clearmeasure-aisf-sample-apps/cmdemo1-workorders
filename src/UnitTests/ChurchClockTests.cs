using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests;

[TestFixture]
public class ChurchClockTests
{
    [Test]
    public void NoonUtcOn_WhenWinterCentralStandardTime_ReturnsEighteenHundredUtc()
    {
        var clock = ChurchClock.NoonUtcOn(2026, 1, 5);

        clock.GetUtcNow().ShouldBe(new DateTimeOffset(2026, 1, 5, 18, 0, 0, TimeSpan.Zero));
        clock.GetUtcNow().Offset.ShouldBe(TimeSpan.Zero);
    }

    [Test]
    public void NoonUtcOn_WhenSummerCentralDaylightTime_ReturnsSeventeenHundredUtc()
    {
        var clock = ChurchClock.NoonUtcOn(2026, 8, 29);

        clock.GetUtcNow().ShouldBe(new DateTimeOffset(2026, 8, 29, 17, 0, 0, TimeSpan.Zero));
        clock.GetUtcNow().Offset.ShouldBe(TimeSpan.Zero);
    }

    [Test]
    public void NoonUtcOn_WhenDayBeforeSpringForward_ReturnsEighteenHundredUtc()
    {
        var clock = ChurchClock.NoonUtcOn(2026, 3, 7);

        clock.GetUtcNow().ShouldBe(new DateTimeOffset(2026, 3, 7, 18, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void NoonUtcOn_WhenSpringForwardDay_ReturnsSeventeenHundredUtc()
    {
        var clock = ChurchClock.NoonUtcOn(2026, 3, 8);

        clock.GetUtcNow().ShouldBe(new DateTimeOffset(2026, 3, 8, 17, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void NoonUtcOn_WhenDayBeforeFallBack_ReturnsSeventeenHundredUtc()
    {
        var clock = ChurchClock.NoonUtcOn(2026, 10, 31);

        clock.GetUtcNow().ShouldBe(new DateTimeOffset(2026, 10, 31, 17, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void NoonUtcOn_WhenFallBackDay_ReturnsEighteenHundredUtc()
    {
        var clock = ChurchClock.NoonUtcOn(2026, 11, 1);

        clock.GetUtcNow().ShouldBe(new DateTimeOffset(2026, 11, 1, 18, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void NoonUtcOn_WhenAnyDate_ChurchTodayIsThatDate()
    {
        var clock = ChurchClock.NoonUtcOn(2026, 8, 29);

        ChurchTimeZone.Today(clock).ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Test]
    public void AtUtc_WhenUtcInstant_ReturnsClockFrozenAtThatInstant()
    {
        var instant = new DateTimeOffset(2026, 10, 13, 18, 0, 0, TimeSpan.Zero);

        var clock = ChurchClock.AtUtc(instant);

        clock.ShouldBeOfType<FixedUtcClock>();
        clock.GetUtcNow().ShouldBe(instant);
    }

    [Test]
    public void AtUtc_WhenOffsetIsNotZero_ThrowsArgumentException()
    {
        var chicagoLocal = new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.FromHours(-6));

        var exception = Should.Throw<ArgumentException>(() => ChurchClock.AtUtc(chicagoLocal));

        exception.ParamName.ShouldBe("utcNow");
    }
}
