using Mediator;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Asks the server for its aggregate health; the Blazor client's server health check sends it over the remotable bus.
/// </summary>
public record ServerHealthCheckQuery : IRequest<HealthStatus>, IRemotableRequest;
