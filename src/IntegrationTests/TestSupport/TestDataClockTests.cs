using ClearMeasure.Bootcamp.UnitTests;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.TestSupport;

[TestFixture]
public class TestDataClockTests
{
    private static readonly DateTimeOffset Instant = new(2026, 6, 15, 18, 0, 0, TimeSpan.Zero);

    [Test]
    public void FixedClock_WhenCalled_ReturnsSharedFixedUtcClockFrozenAtInstant()
    {
        var clock = TestData.FixedClock(Instant);

        clock.ShouldBeOfType<FixedUtcClock>();
        clock.GetUtcNow().ShouldBe(Instant);
        clock.LocalTimeZone.ShouldBe(TimeZoneInfo.Utc);
    }
}
