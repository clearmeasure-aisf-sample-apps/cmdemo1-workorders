using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record NotificationsPanelQuery(string? UserName) : IRequest<NotificationsPanel>, IRemotableRequest;

public record NotificationsPanel(
    WorkOrder[] OverdueAndDueToday,
    WorkOrder[] NewlyAssignedLast7Days,
    WorkOrder[] CreatedByMeStatusChangedLast7Days);
