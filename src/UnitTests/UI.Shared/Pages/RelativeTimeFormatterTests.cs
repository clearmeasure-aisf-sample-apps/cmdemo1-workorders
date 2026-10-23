using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class RelativeTimeFormatterTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 15, 0, 0, TimeSpan.Zero);

    [Test]
    public void Format_WhenCreatedIsNull_ReturnsNull()
    {
        RelativeTimeFormatter.Format(null, Clock()).ShouldBeNull();
    }

    [Test]
    public void Format_WhenUnderOneMinute_ReturnsJustNow()
    {
        var created = Now.UtcDateTime.AddSeconds(-30);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("just now");
    }

    [Test]
    public void Format_WhenOneMinute_ReturnsSingular()
    {
        var created = Now.UtcDateTime.AddMinutes(-1);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("1 minute ago");
    }

    [Test]
    public void Format_WhenThreeMinutes_ReturnsPlural()
    {
        var created = Now.UtcDateTime.AddMinutes(-3);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("3 minutes ago");
    }

    [Test]
    public void Format_WhenOneHour_ReturnsSingular()
    {
        var created = Now.UtcDateTime.AddHours(-1);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("1 hour ago");
    }

    [Test]
    public void Format_WhenFiveHours_ReturnsPlural()
    {
        var created = Now.UtcDateTime.AddHours(-5);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("5 hours ago");
    }

    [Test]
    public void Format_WhenOneDay_ReturnsSingular()
    {
        var created = Now.UtcDateTime.AddDays(-1);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("1 day ago");
    }

    [Test]
    public void Format_WhenThreeDays_ReturnsPlural()
    {
        var created = Now.UtcDateTime.AddDays(-3);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("3 days ago");
    }

    [Test]
    public void Format_WhenThirtyDays_ReturnsOneMonthAgo()
    {
        var created = Now.UtcDateTime.AddDays(-30);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("1 month ago");
    }

    [Test]
    public void Format_WhenNinetyDays_ReturnsThreeMonthsAgo()
    {
        var created = Now.UtcDateTime.AddDays(-90);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("3 months ago");
    }

    [Test]
    public void Format_WhenOneSecondInTheFuture_ReturnsJustNow()
    {
        var created = Now.UtcDateTime.AddSeconds(1);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("just now");
    }

    [Test]
    public void Format_WhenCreatedIsUnspecified_TreatsItAsUtc()
    {
        var created = DateTime.SpecifyKind(Now.UtcDateTime.AddDays(-3), DateTimeKind.Unspecified);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("3 days ago");
    }

    private static TimeProvider Clock() => new FixedTimeProvider(Now);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
