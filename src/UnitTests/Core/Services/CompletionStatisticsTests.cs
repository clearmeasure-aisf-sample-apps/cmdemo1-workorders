using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class CompletionStatisticsTests
{
    [Test]
    public void Should_CountZeroAndAverageNull_When_NoCompletions()
    {
        var statistics = new CompletionStatistics([]);

        statistics.Count.ShouldBe(0);
        statistics.AverageDaysToClose.ShouldBeNull();
    }

    [Test]
    public void Should_AverageDaysToClose_When_CompletionsHaveSpans()
    {
        var statistics = new CompletionStatistics([
            new CompletionSpan(new DateTime(2026, 10, 1), new DateTime(2026, 10, 3)),
            new CompletionSpan(new DateTime(2026, 10, 1), new DateTime(2026, 10, 5))
        ]);

        statistics.Count.ShouldBe(2);
        statistics.AverageDaysToClose.ShouldBe(3.0);
    }

    [Test]
    public void Should_CountEveryCompletionButAverageOnlySpans_When_SomeInstantsMissing()
    {
        var statistics = new CompletionStatistics([
            new CompletionSpan(new DateTime(2026, 10, 1), new DateTime(2026, 10, 2)),
            new CompletionSpan(null, new DateTime(2026, 10, 5))
        ]);

        statistics.Count.ShouldBe(2);
        statistics.AverageDaysToClose.ShouldBe(1.0);
    }

    [Test]
    public void Should_AverageBeNull_When_NoCompletionHasBothInstants()
    {
        var statistics = new CompletionStatistics([new CompletionSpan(null, new DateTime(2026, 10, 5))]);

        statistics.Count.ShouldBe(1);
        statistics.AverageDaysToClose.ShouldBeNull();
    }
}
