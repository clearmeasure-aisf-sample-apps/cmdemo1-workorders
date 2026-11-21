using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class RelativeTimeFormatterWeeksTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 15, 0, 0, TimeSpan.Zero);

    [Test]
    public void Format_WhenSixDays_ReturnsSixDaysAgo()
    {
        var created = Now.UtcDateTime.AddDays(-6);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("6 days ago");
    }

    [Test]
    public void Format_WhenSevenDays_ReturnsOneWeekAgo()
    {
        var created = Now.UtcDateTime.AddDays(-7);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("1 week ago");
    }

    [Test]
    public void Format_WhenTwentyDays_ReturnsTwoWeeksAgo()
    {
        var created = Now.UtcDateTime.AddDays(-20);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("2 weeks ago");
    }

    [Test]
    public void Format_WhenTwentyNineDays_ReturnsFourWeeksAgo()
    {
        var created = Now.UtcDateTime.AddDays(-29);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("4 weeks ago");
    }

    private static TimeProvider Clock() => new FixedTimeProvider(Now);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
