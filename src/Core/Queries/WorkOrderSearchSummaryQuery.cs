using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns the summary counts for the Work Order Search cards and navigation badges.
/// User-dependent counts are zero when <paramref name="UserName"/> is null or unknown.
/// </summary>
public record WorkOrderSearchSummaryQuery(string? UserName) : IRequest<WorkOrderSearchSummary>, IRemotableRequest;
