using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Queries the SLA health metrics for the current month and last month.
/// SLA = share of work orders completed within the month whose CompletedDate &lt;= DueDate.
/// Orders without DueDate are excluded; months use ChurchTimeZone.
/// </summary>
public record SlaHealthQuery : IRequest<SlaHealth>, IRemotableRequest;
