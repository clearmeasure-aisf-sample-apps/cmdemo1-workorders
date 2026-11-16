using ClearMeasure.Bootcamp.Core.Model;
using Mediator;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record NotificationsPanelQuery(string? UserName) : IRequest<NotificationsSummary>, IRemotableRequest;

/// <summary>
/// Notification lists for the shell bell panel (overdue, newly assigned, created-by-me changes).
/// </summary>
public record NotificationsSummary(
    WorkOrder[] OverdueAndDueToday,
    WorkOrder[] NewlyAssignedLast7Days,
    WorkOrder[] CreatedByMeStatusChangedLast7Days);
