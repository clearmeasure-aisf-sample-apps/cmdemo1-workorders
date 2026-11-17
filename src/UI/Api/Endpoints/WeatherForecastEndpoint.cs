using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// <c>GET WeatherForecast</c> (and <c>api/v1.0/WeatherForecast</c>): the template's sample forecast, kept for demos.
/// </summary>
public static class WeatherForecastEndpoint
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public static void Map(IEndpointRouteBuilder app)
    {
        foreach (var route in new[] { "WeatherForecast", $"{ApiRoutes.VersionedApiPrefix}/WeatherForecast" })
        {
            app.MapGet(route, Get).CacheOutput(OutputCachePolicyNames.WeatherSample);
        }
    }

    /// <summary>
    /// Returns a JSON array of five sample weather-forecast entries (output-cached).
    /// </summary>
    public static IResult Get(HttpContext context, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(typeof(WeatherForecastEndpoint).FullName!);
        logger.LogDebug("LogDebug Get Weather");
        logger.LogInformation("LogInformation Get Weather");
        logger.LogTrace("LogTrace Get Weather");

        var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        return ConditionalGetEtag.ConditionalJson(context, forecasts);
    }
}
