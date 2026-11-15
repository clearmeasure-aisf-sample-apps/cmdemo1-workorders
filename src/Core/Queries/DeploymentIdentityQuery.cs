using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Loads the process deployment identity for the app shell footer.
/// </summary>
public record DeploymentIdentityQuery : IRequest<DeploymentIdentity>, IRemotableRequest;
