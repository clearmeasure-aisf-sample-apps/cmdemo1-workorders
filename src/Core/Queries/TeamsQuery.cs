using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns every team with its members, ordered by name ignoring case.
/// </summary>
public record TeamsQuery : IRequest<Team[]>, IRemotableRequest;
