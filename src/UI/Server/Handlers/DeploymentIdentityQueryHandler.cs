using ClearMeasure.Bootcamp.Core.Queries;
using MediatR;

namespace ClearMeasure.Bootcamp.UI.Server.Handlers;

/// <summary>
/// Answers <see cref="DeploymentIdentityQuery"/> from the running process (same sources as the environment status API).
/// </summary>
public class DeploymentIdentityQueryHandler : IRequestHandler<DeploymentIdentityQuery, DeploymentIdentity>
{
    public Task<DeploymentIdentity> Handle(DeploymentIdentityQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(DeploymentIdentity.FromProcess());
}
