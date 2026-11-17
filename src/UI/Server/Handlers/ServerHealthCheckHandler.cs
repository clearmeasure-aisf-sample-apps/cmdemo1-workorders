using ClearMeasure.Bootcamp.Core.Queries;
using Mediator;

namespace ClearMeasure.Bootcamp.UI.Server.Handlers;

public class ServerHealthCheckHandler(HealthCheckService healthCheckService)
    : IRequestHandler<ServerHealthCheckQuery, HealthStatus>
{
    public async ValueTask<HealthStatus> Handle(ServerHealthCheckQuery request, CancellationToken cancellationToken)
    {
        var checkHealthAsync = await healthCheckService.CheckHealthAsync(cancellationToken);
        return checkHealthAsync.Status;
    }
}