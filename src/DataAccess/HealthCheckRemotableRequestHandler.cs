using ClearMeasure.Bootcamp.Core;
using Mediator;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.DataAccess;

public class HealthCheckRemotableRequestHandler : IRequestHandler<HealthCheckRemotableRequest, HealthStatus>
{
    public ValueTask<HealthStatus> Handle(HealthCheckRemotableRequest request, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(request.Status);
    }
}