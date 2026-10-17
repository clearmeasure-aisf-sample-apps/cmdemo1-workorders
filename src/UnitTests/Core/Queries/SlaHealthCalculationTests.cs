using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;

[TestFixture]
public class SlaHealthCalculationTests
{
    [Test]
    public void Should_ReturnNullPercentage_When_NoQualifyingOrders()
    {
        var result = SlaHealth.TrendText(null, null);

        result.ShouldBeNull();
    }

    [Test]
    public void Should_ReturnNullTrendText_When_CurrentMonthHasData_ButLastMonthDoesNot()
    {
        var result = SlaHealth.TrendText(80m, null);

        result.ShouldBeNull();
    }

    [Test]
    public void Should_ReturnTrendUp_When_CurrentPercentageGreaterThanLastMonth()
    {
        var result = SlaHealth.TrendText(90m, 75m);

        result.ShouldBe("↑ up from 75%");
    }

    [Test]
    public void Should_ReturnTrendDown_When_CurrentPercentageLessThanLastMonth()
    {
        var result = SlaHealth.TrendText(60m, 80m);

        result.ShouldBe("↓ down from 80%");
    }

    [Test]
    public void Should_ReturnTrendFlat_When_PercentagesEqual()
    {
        var result = SlaHealth.TrendText(75m, 75m);

        result.ShouldBe("— flat at 75%");
    }

    [Test]
    public void Should_RoundPercentageToWholeNumber()
    {
        // Test rounding with TrendText
        var result = SlaHealth.TrendText(66.67m, 50m);

        result.ShouldBe("↑ up from 50%");
    }

    [Test]
    public void Should_HandleZeroCurrentPercentage()
    {
        var result = SlaHealth.TrendText(0m, 50m);

        result.ShouldBe("↓ down from 50%");
    }

    [Test]
    public void Should_Handle100Percent()
    {
        var result = SlaHealth.TrendText(100m, 90m);

        result.ShouldBe("↑ up from 90%");
    }

    private static WorkOrder Order(DateOnly? due, DateTime? completed) =>
        new() { DueDate = due, CompletedDate = completed };

    [Test]
    public void Should_ReturnNullPercentage_When_ZeroOrders()
    {
        var (percentage, count) = SlaHealth.CalculateOnTimePercentage([]);

        percentage.ShouldBeNull();
        count.ShouldBe(0);
    }

    [Test]
    public void Should_ExcludeOrders_When_NoDueDate()
    {
        var (percentage, count) = SlaHealth.CalculateOnTimePercentage(
        [
            Order(null, new DateTime(2026, 5, 10)),
            Order(new DateOnly(2026, 5, 10), new DateTime(2026, 5, 11, 12, 0, 0))
        ]);

        percentage.ShouldBe(0m);
        count.ShouldBe(1);
    }

    [Test]
    public void Should_ReturnNullPercentage_When_AllOrdersHaveNoDueDate()
    {
        var (percentage, count) = SlaHealth.CalculateOnTimePercentage([Order(null, new DateTime(2026, 5, 10))]);

        percentage.ShouldBeNull();
        count.ShouldBe(0);
    }

    [Test]
    public void Should_CountAsOnTime_When_CompletedOnDueDate()
    {
        var (percentage, count) = SlaHealth.CalculateOnTimePercentage(
            [Order(new DateOnly(2026, 5, 10), new DateTime(2026, 5, 10, 23, 59, 0))]);

        percentage.ShouldBe(100m);
        count.ShouldBe(1);
    }

    [Test]
    public void Should_CountAsLate_When_CompletedAfterDueDate()
    {
        var (percentage, _) = SlaHealth.CalculateOnTimePercentage(
            [Order(new DateOnly(2026, 5, 10), new DateTime(2026, 5, 11, 5, 1, 0))]);

        percentage.ShouldBe(0m);
    }

    [Test]
    public void Should_CountAsOnTime_When_CompletedEveningLocal_NextDayUtc()
    {
        var (percentage, count) = SlaHealth.CalculateOnTimePercentage(
            [Order(new DateOnly(2026, 10, 15), new DateTime(2026, 10, 16, 1, 0, 0))]);

        percentage.ShouldBe(100m);
        count.ShouldBe(1);
    }

    [Test]
    public void Should_ReturnChurchDate_When_CompletedLateEveningLocal()
    {
        var result = SlaHealth.ChurchDate(new DateTime(2026, 11, 1, 3, 0, 0));

        result.ShouldBe(new DateOnly(2026, 10, 31));
    }

    [Test]
    public void Should_RoundMixedPercentage_ToWholeNumber()
    {
        var due = new DateOnly(2026, 5, 10);
        var (percentage, count) = SlaHealth.CalculateOnTimePercentage(
        [
            Order(due, new DateTime(2026, 5, 9)),
            Order(due, new DateTime(2026, 5, 10)),
            Order(due, new DateTime(2026, 5, 12))
        ]);

        percentage.ShouldBe(67m);
        count.ShouldBe(3);
    }

    [Test]
    public void Should_ComputeTrend_FromCalculatedPercentages()
    {
        var due = new DateOnly(2026, 5, 10);
        var (current, _) = SlaHealth.CalculateOnTimePercentage(
            [Order(due, new DateTime(2026, 5, 9)), Order(due, new DateTime(2026, 5, 10))]);
        var (last, _) = SlaHealth.CalculateOnTimePercentage(
            [Order(due, new DateTime(2026, 5, 9)), Order(due, new DateTime(2026, 5, 12))]);

        SlaHealth.TrendText(current, last).ShouldBe("↑ up from 50%");
        SlaHealth.TrendText(last, current).ShouldBe("↓ down from 100%");
    }
}
