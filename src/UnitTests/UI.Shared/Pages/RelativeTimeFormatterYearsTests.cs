using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class RelativeTimeFormatterYearsTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 15, 0, 0, TimeSpan.Zero);

    [Test]
    public void Format_When364Days_Returns12MonthsAgo()
    {
        var created = Now.UtcDateTime.AddDays(-364);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("12 months ago");
    }

    [Test]
    public void Format_When365Days_Returns1YearAgo()
    {
        var created = Now.UtcDateTime.AddDays(-365);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("1 year ago");
    }

    [Test]
    public void Format_When800Days_Returns2YearsAgo()
    {
        var created = Now.UtcDateTime.AddDays(-800);

        RelativeTimeFormatter.Format(created, Clock()).ShouldBe("2 years ago");
    }

    private static TimeProvider Clock() => new FixedTimeProvider(Now);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
