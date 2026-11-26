using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class SlaCalculatorTests
{
    [Test]
    public void Should_ReturnUnknownTrend_When_BothPercentagesAreNull()
    {
        var result = SlaCalculator.Trend(null, null);

        result.Trend.ShouldBe(SlaTrend.Unknown);
        result.ComparedPercentage.ShouldBeNull();
    }

    [Test]
    public void Should_ReturnUnknownTrend_When_CurrentMonthHasData_ButLastMonthDoesNot()
    {
        var result = SlaCalculator.Trend(80m, null);

        result.Trend.ShouldBe(SlaTrend.Unknown);
        result.ComparedPercentage.ShouldBeNull();
    }

    [Test]
    public void Should_ReturnTrendUp_When_CurrentPercentageGreaterThanLastMonth()
    {
        var result = SlaCalculator.Trend(90m, 75m);

        result.Trend.ShouldBe(SlaTrend.Up);
        result.ComparedPercentage.ShouldBe(75m);
    }

    [Test]
    public void Should_ReturnTrendDown_When_CurrentPercentageLessThanLastMonth()
    {
        var result = SlaCalculator.Trend(60m, 80m);

        result.Trend.ShouldBe(SlaTrend.Down);
        result.ComparedPercentage.ShouldBe(80m);
    }

    [Test]
    public void Should_ReturnTrendFlat_When_PercentagesEqual()
    {
        var result = SlaCalculator.Trend(75m, 75m);

        result.Trend.ShouldBe(SlaTrend.Flat);
        result.ComparedPercentage.ShouldBe(75m);
    }

    [Test]
    public void Should_ReturnTrendUp_When_CurrentPercentageIsFractional()
    {
        var result = SlaCalculator.Trend(66.67m, 50m);

        result.Trend.ShouldBe(SlaTrend.Up);
        result.ComparedPercentage.ShouldBe(50m);
    }

    [Test]
    public void Should_ReturnTrendDown_When_CurrentPercentageIsZero()
    {
        var result = SlaCalculator.Trend(0m, 50m);

        result.Trend.ShouldBe(SlaTrend.Down);
        result.ComparedPercentage.ShouldBe(50m);
    }

    [Test]
    public void Should_ReturnTrendUp_When_CurrentPercentageIs100()
    {
        var result = SlaCalculator.Trend(100m, 90m);

        result.Trend.ShouldBe(SlaTrend.Up);
        result.ComparedPercentage.ShouldBe(90m);
    }

    [Test]
    public void Should_ReturnNullPercentage_When_ZeroCompletions()
    {
        var (percentage, count) = SlaCalculator.CalculateOnTimePercentage(Enumerable.Empty<SlaCompletion>());

        percentage.ShouldBeNull();
        count.ShouldBe(0);
    }

    [Test]
    public void Should_CountAsOnTime_When_CompletedOnDueDate()
    {
        var (percentage, count) = SlaCalculator.CalculateOnTimePercentage(
        [
            new SlaCompletion(new DateOnly(2026, 5, 10), new DateTime(2026, 5, 10, 23, 59, 0))
        ]);

        percentage.ShouldBe(100m);
        count.ShouldBe(1);
    }

    [Test]
    public void Should_CountAsLate_When_CompletedAfterDueDate()
    {
        var (percentage, _) = SlaCalculator.CalculateOnTimePercentage(
        [
            new SlaCompletion(new DateOnly(2026, 5, 10), new DateTime(2026, 5, 11, 5, 1, 0))
        ]);

        percentage.ShouldBe(0m);
    }

    [Test]
    public void Should_CountAsOnTime_When_CompletedEveningLocal_NextDayUtc()
    {
        var (percentage, count) = SlaCalculator.CalculateOnTimePercentage(
        [
            new SlaCompletion(new DateOnly(2026, 10, 15), new DateTime(2026, 10, 16, 1, 0, 0))
        ]);

        percentage.ShouldBe(100m);
        count.ShouldBe(1);
    }

    [Test]
    public void Should_RoundMixedPercentage_ToWholeNumber()
    {
        var due = new DateOnly(2026, 5, 10);
        var (percentage, count) = SlaCalculator.CalculateOnTimePercentage(
        [
            new SlaCompletion(due, new DateTime(2026, 5, 9)),
            new SlaCompletion(due, new DateTime(2026, 5, 10)),
            new SlaCompletion(due, new DateTime(2026, 5, 12))
        ]);

        percentage.ShouldBe(67m);
        count.ShouldBe(3);
    }

    [TestCase(1, 12)]
    [TestCase(3, 38)]
    [TestCase(5, 62)]
    [TestCase(7, 88)]
    public void Should_RoundMidpointPercentage_ToEven(int onTimeOfEight, decimal expectedPercentage)
    {
        var due = new DateOnly(2026, 5, 10);
        var onTime = Enumerable.Range(0, onTimeOfEight)
            .Select(_ => new SlaCompletion(due, new DateTime(2026, 5, 9)));
        var late = Enumerable.Range(0, 8 - onTimeOfEight)
            .Select(_ => new SlaCompletion(due, new DateTime(2026, 5, 12)));
        var completions = onTime.Concat(late).ToList();

        var (percentage, count) = SlaCalculator.CalculateOnTimePercentage(completions);

        percentage.ShouldBe(expectedPercentage);
        count.ShouldBe(8);
    }

    [Test]
    public void Should_ComputeTrend_FromCalculatedPercentages()
    {
        var due = new DateOnly(2026, 5, 10);
        var (current, _) = SlaCalculator.CalculateOnTimePercentage(
        [
            new SlaCompletion(due, new DateTime(2026, 5, 9)),
            new SlaCompletion(due, new DateTime(2026, 5, 10))
        ]);
        var (last, _) = SlaCalculator.CalculateOnTimePercentage(
        [
            new SlaCompletion(due, new DateTime(2026, 5, 9)),
            new SlaCompletion(due, new DateTime(2026, 5, 12))
        ]);

        SlaCalculator.Trend(current, last).ShouldBe(new SlaTrendComparison(SlaTrend.Up, 50m));
        SlaCalculator.Trend(last, current).ShouldBe(new SlaTrendComparison(SlaTrend.Down, 100m));
    }
}
