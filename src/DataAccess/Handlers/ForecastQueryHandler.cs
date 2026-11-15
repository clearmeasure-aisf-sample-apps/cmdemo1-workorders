using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using MediatR;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class ForecastQueryHandler(TimeProvider timeProvider) : IRequestHandler<ForecastQuery, WeatherForecast[]>
{
    public Task<WeatherForecast[]> Handle(ForecastQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new WeatherForecastData(timeProvider).GetAll());
    }

    private class WeatherForecastData
    {
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        private readonly WeatherForecast[] _allForecasts;

        public WeatherForecastData(TimeProvider timeProvider)
        {
            var now = timeProvider.GetLocalNow().DateTime;
            var forecasts = new List<WeatherForecast>();
            for (var i = 1; i <= 5; i++)
            {
                var forecast = new WeatherForecast
                {
                    Date = now.AddDays(i),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)],
                    TemperatureC = Random.Shared.Next(-20, 55)
                };
                forecasts.Add(forecast);
            }

            _allForecasts = forecasts.ToArray();
        }

        public WeatherForecast[] GetAll()
        {
            return _allForecasts;
        }
    }
}
