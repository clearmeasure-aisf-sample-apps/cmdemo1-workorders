using ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;
using Shouldly;
using static ClearMeasure.Bootcamp.UI.Server.LiveTelemetry.RollingCounts;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.LiveTelemetry;

[TestFixture]
public class RollingCountsTests
{
    [Test]
    public void ShouldSumTheSecondsOfTheWindow_WhenTotalsAreRead()
    {
        var counts = new RollingCounts(windowSeconds: 60);
        counts.Increment(1_000, Counter.Probes);
        counts.Increment(30_500, Counter.Probes);
        counts.Increment(30_900, Counter.HttpClientCalls);

        var totals = counts.Totals(nowSecond: 59);

        totals.Probes().PerMinute.ShouldBe(2);
        totals.HttpClient().PerMinute.ShouldBe(1);
    }

    [Test]
    public void ShouldDropASecond_WhenItLeavesTheWindow()
    {
        var counts = new RollingCounts(windowSeconds: 60);
        counts.Increment(1_000, Counter.Probes);

        var totals = counts.Totals(nowSecond: 61);

        totals.Probes().PerMinute.ShouldBe(0);
    }

    [Test]
    public void ShouldStartABucketAgain_WhenItsSecondComesRoundOnceMore()
    {
        var counts = new RollingCounts(windowSeconds: 60);
        counts.Increment(1_000, Counter.Probes);
        counts.Increment(61_000, Counter.Probes);

        var totals = counts.Totals(nowSecond: 61);

        totals.Probes().PerMinute.ShouldBe(1);
    }
}
