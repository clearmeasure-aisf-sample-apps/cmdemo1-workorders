using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class ChurchCalendarTests
{
    private sealed class StubTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private static IEnumerable<DateTimeOffset> FixedClocks()
    {
        yield return new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);
        yield return new DateTimeOffset(2026, 3, 8, 7, 59, 0, TimeSpan.Zero);
        yield return new DateTimeOffset(2026, 3, 8, 8, 0, 0, TimeSpan.Zero);
        yield return new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero);
        yield return new DateTimeOffset(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);
        yield return new DateTimeOffset(2026, 8, 30, 3, 0, 0, TimeSpan.Zero);
        yield return new DateTimeOffset(2026, 11, 1, 7, 0, 0, TimeSpan.Zero);
        yield return new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    [TestCaseSource(nameof(FixedClocks))]
    public void Today_FixedClock_ShouldEqualChurchTimeZoneToday(DateTimeOffset utcNow)
    {
        var clock = new StubTimeProvider(utcNow);

        new ChurchCalendar(clock).Today.ShouldBe(ChurchTimeZone.Today(clock));
    }

    [TestCaseSource(nameof(FixedClocks))]
    public void ComingSaturday_FixedClock_ShouldEqualChurchTimeZoneComingSaturday(DateTimeOffset utcNow)
    {
        var clock = new StubTimeProvider(utcNow);

        new ChurchCalendar(clock).ComingSaturday.ShouldBe(ChurchTimeZone.ComingSaturday(clock));
    }

    [Test]
    public void Today_ClockAdvances_ShouldReadCurrentInstantEachTime()
    {
        var clock = new AdvancingTimeProvider(new DateTimeOffset(2026, 8, 29, 12, 0, 0, TimeSpan.Zero));
        var calendar = new ChurchCalendar(clock);
        var first = calendar.Today;

        clock.Now = clock.Now.AddDays(1);

        calendar.Today.ShouldBe(first.AddDays(1));
    }

    [Test]
    public void Constructor_NullTimeProvider_ShouldThrowArgumentNullException()
    {
        var exception = Should.Throw<ArgumentNullException>(() => new ChurchCalendar(null!));

        exception.ParamName.ShouldBe("timeProvider");
    }

    private sealed class AdvancingTimeProvider(DateTimeOffset start) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = start;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
