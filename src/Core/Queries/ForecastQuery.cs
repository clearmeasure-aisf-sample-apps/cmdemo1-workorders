using ClearMeasure.Bootcamp.Core.Model;
using Mediator;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record ForecastQuery : IRequest<WeatherForecast[]>, IRemotableRequest;
