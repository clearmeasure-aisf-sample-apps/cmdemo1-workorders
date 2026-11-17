using ClearMeasure.Bootcamp.Core.Queries;
using Mediator;

namespace ClearMeasure.Bootcamp.UI.Server.Handlers;

/// <summary>
/// Answers <see cref="DeploymentIdentityQuery"/> from the running process (same sources as the environment status API).
/// </summary>
public class DeploymentIdentityQueryHandler : IRequestHandler<DeploymentIdentityQuery, DeploymentIdentity>
{
    public ValueTask<DeploymentIdentity> Handle(DeploymentIdentityQuery request, CancellationToken cancellationToken) =>
        ValueTask.FromResult(DeploymentIdentity.FromProcess());
}
