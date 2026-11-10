using ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.LiveTelemetry;

[TestFixture]
public class LatencySamplesTests
{
    [Test]
    public void ShouldAnswerTheNearestRank95thPercentile_WhenTwentySamplesAreInTheWindow()
    {
        var samples = new LatencySamples(capacity: 32);
        LatencySteps.AddOneToTwenty(samples, atMs: 1_000);

        var p95 = samples.Percentile95(afterMs: 0);

        p95.ShouldBe(19);
    }

    [Test]
    public void ShouldAnswerNull_WhenEverySampleIsOlderThanTheWindow()
    {
        var samples = new LatencySamples(capacity: 4);
        samples.Add(1_000, 50);

        var p95 = samples.Percentile95(afterMs: 1_000);

        p95.ShouldBeNull();
    }

    [Test]
    public void ShouldKeepOnlyTheLatestSamples_WhenTheBufferIsFull()
    {
        var samples = new LatencySamples(capacity: 2);
        samples.Add(1_000, 900);
        samples.Add(1_001, 10);
        samples.Add(1_002, 20);

        var p95 = samples.Percentile95(afterMs: 0);

        p95.ShouldBe(20);
    }
}
