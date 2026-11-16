using Mediator;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.Core;

public record HealthCheckRemotableRequest(HealthStatus Status = HealthStatus.Healthy)
    : IRequest<HealthStatus>, IRemotableRequest;
