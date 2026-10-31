using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns the facility's work order figures: open counts by status/priority/building,
/// completed per week for the last 8 weeks, average days to close in the last 30 days,
/// and the current overdue count.
/// </summary>
public record WorkOrderReportsQuery : IRequest<WorkOrderReportsData>, IRemotableRequest;
