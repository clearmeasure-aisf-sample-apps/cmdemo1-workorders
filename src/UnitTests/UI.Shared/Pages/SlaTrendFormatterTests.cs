using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class SlaTrendFormatterTests
{
    [Test]
    public void Format_WhenTrendIsUp_ReturnsUpArrowAndComparedPercentage()
    {
        SlaTrendFormatter.Format(new SlaTrendComparison(SlaTrend.Up, 75m)).ShouldBe("↑ up from 75%");
    }

    [Test]
    public void Format_WhenTrendIsDown_ReturnsDownArrowAndComparedPercentage()
    {
        SlaTrendFormatter.Format(new SlaTrendComparison(SlaTrend.Down, 80m)).ShouldBe("↓ down from 80%");
    }

    [Test]
    public void Format_WhenTrendIsFlat_ReturnsDashAndComparedPercentage()
    {
        SlaTrendFormatter.Format(new SlaTrendComparison(SlaTrend.Flat, 75m)).ShouldBe("— flat at 75%");
    }

    [Test]
    public void Format_WhenTrendIsUnknown_ReturnsNull()
    {
        SlaTrendFormatter.Format(new SlaTrendComparison(SlaTrend.Unknown, null)).ShouldBeNull();
    }

    [Test]
    public void Format_WhenComparedPercentageIsFractional_RoundsToWholeNumber()
    {
        SlaTrendFormatter.Format(new SlaTrendComparison(SlaTrend.Up, 66.67m)).ShouldBe("↑ up from 67%");
    }

    [TestCase(90, 75, "↑ up from 75%")]
    [TestCase(60, 80, "↓ down from 80%")]
    [TestCase(75, 75, "— flat at 75%")]
    [TestCase(0, 50, "↓ down from 50%")]
    [TestCase(100, 90, "↑ up from 90%")]
    public void Format_WhenGivenCalculatorTrend_ReturnsCardText(int current, int last, string expected)
    {
        SlaTrendFormatter.Format(SlaCalculator.Trend(current, last)).ShouldBe(expected);
    }

    [Test]
    public void Format_WhenCalculatorHasNoLastMonth_ReturnsNull()
    {
        SlaTrendFormatter.Format(SlaCalculator.Trend(80m, null)).ShouldBeNull();
    }
}
