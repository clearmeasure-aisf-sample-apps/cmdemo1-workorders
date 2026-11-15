using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class ForecastQueryHandlerTests
{
    [Test]
    public async Task Handle_ShouldReturnFiveForecasts_WithRequiredFields()
    {
        var clock = TimeProvider.System;
        var handler = new ForecastQueryHandler(clock);

        var forecasts = await handler.Handle(new ForecastQuery(), CancellationToken.None);

        forecasts.ShouldNotBeNull();
        forecasts.Length.ShouldBe(5);
        var today = clock.GetUtcNow().DateTime.Date;
        foreach (var forecast in forecasts)
        {
            forecast.Date.ShouldBeGreaterThan(today);
            forecast.Summary.ShouldNotBeNullOrWhiteSpace();
            forecast.TemperatureC.ShouldBeInRange(-20, 54);
        }
    }

    [Test]
    public async Task Handle_ShouldBeResolvableFromTestHost()
    {
        var handler = TestHost.GetRequiredService<ForecastQueryHandler>();

        var forecasts = await handler.Handle(new ForecastQuery(), CancellationToken.None);

        forecasts.Length.ShouldBe(5);
    }
}
