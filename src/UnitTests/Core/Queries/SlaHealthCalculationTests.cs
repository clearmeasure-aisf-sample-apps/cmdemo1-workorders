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
}
