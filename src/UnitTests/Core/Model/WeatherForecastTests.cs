using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WeatherForecastTests
{
    [Test]
    public void SummaryDisplay_WhenSummaryIsNull_ReturnsNoSummary()
    {
        var forecast = new WeatherForecast { Summary = null };

        forecast.SummaryDisplay.ShouldBe("No summary");
    }

    [Test]
    public void SummaryDisplay_WhenSummaryIsWhitespace_ReturnsNoSummary()
    {
        var forecast = new WeatherForecast { Summary = "   \t  " };

        forecast.SummaryDisplay.ShouldBe("No summary");
    }

    [Test]
    public void SummaryDisplay_WhenSummaryIsNonBlank_ReturnsSummaryAsIs()
    {
        var forecast = new WeatherForecast { Summary = "Mild" };

        forecast.SummaryDisplay.ShouldBe("Mild");
    }
}
