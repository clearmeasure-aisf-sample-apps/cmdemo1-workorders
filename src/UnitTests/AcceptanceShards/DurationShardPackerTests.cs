using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceShards;

[TestFixture]
public class DurationShardPackerTests
{
    [Test]
    public void Should_SpreadLongClasses_AcrossShards()
    {
        var classes = new[]
        {
            new DurationClass("A", 100, Tests: 2),
            new DurationClass("B", 90),
            new DurationClass("C", 80),
            new DurationClass("D", 10),
            new DurationClass("E", 10),
            new DurationClass("F", 10)
        };
        classes[0].Tests.ShouldBe(2);

        var buckets = DurationShardPacker.Pack(classes, shardCount: 3);

        buckets.Count.ShouldBe(3);
        buckets.Select(b => b.Classes.Count).Sum().ShouldBe(6);
        buckets.Max(b => b.TotalSeconds).ShouldBeLessThanOrEqualTo(110);
        buckets.Min(b => b.TotalSeconds).ShouldBeGreaterThanOrEqualTo(90);
    }

    [Test]
    public void Should_FillTwelveShards_WhenAskedForTwelve()
    {
        var classes = Enumerable.Range(1, 60)
            .Select(i => new DurationClass($"Class{i:D2}", 20 + i % 7))
            .ToArray();

        var buckets = DurationShardPacker.Pack(classes, shardCount: 12);

        buckets.Count.ShouldBe(12);
        buckets.ShouldAllBe(b => b.Classes.Count > 0);
        var totals = buckets.Select(b => b.TotalSeconds).ToArray();
        (totals.Max() / totals.Min()).ShouldBeLessThan(1.5);
    }
}
