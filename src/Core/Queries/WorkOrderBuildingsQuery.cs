using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns the distinct, non-blank building names used by work orders, trimmed and sorted.
/// </summary>
public record WorkOrderBuildingsQuery : IRequest<string[]>, IRemotableRequest;
