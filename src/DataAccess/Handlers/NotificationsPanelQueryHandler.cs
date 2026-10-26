using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class NotificationsPanelQueryHandler(DataContext context, TimeProvider time)
    : IRequestHandler<NotificationsPanelQuery, NotificationsPanel>
{
    public async Task<NotificationsPanel> Handle(
        NotificationsPanelQuery request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return new NotificationsPanel([], [], []);
        }

        var employee = await context.Set<Employee>()
            .FirstOrDefaultAsync(e => e.UserName == request.UserName, cancellationToken);

        if (employee == null)
        {
            return new NotificationsPanel([], [], []);
        }

        var overdue = await GetOverdueAndDueTodayAsync(employee, cancellationToken);
        var newlyAssigned = await GetNewlyAssignedLast7DaysAsync(employee, cancellationToken);
        var createdByMeChanged = await GetCreatedByMeStatusChangedLast7DaysAsync(employee, cancellationToken);

        return new NotificationsPanel(overdue, newlyAssigned, createdByMeChanged);
    }

    private async Task<WorkOrder[]> GetOverdueAndDueTodayAsync(
        Employee employee,
        CancellationToken cancellationToken)
    {
        var today = ChurchTimeZone.Today(time);
        var query = context.Set<WorkOrder>()
            .Where(wo => wo.Assignee == employee)
            .Where(wo => wo.Status == WorkOrderStatus.Draft ||
                        wo.Status == WorkOrderStatus.Assigned ||
                        wo.Status == WorkOrderStatus.InProgress)
            .Where(wo => wo.DueDate <= today)
            .OrderByDescending(wo => wo.DueDate)
            .ThenByDescending(wo => wo.CreatedDate);

        return await query.ToArrayAsync(cancellationToken);
    }

    private async Task<WorkOrder[]> GetNewlyAssignedLast7DaysAsync(
        Employee employee,
        CancellationToken cancellationToken)
    {
        var sevenDaysAgo = time.GetUtcNow().DateTime.AddDays(-7);
        var query = context.Set<WorkOrder>()
            .Where(wo => wo.Assignee == employee)
            .Where(wo => wo.AssignedDate >= sevenDaysAgo)
            .OrderByDescending(wo => wo.AssignedDate);

        return await query.ToArrayAsync(cancellationToken);
    }

    private async Task<WorkOrder[]> GetCreatedByMeStatusChangedLast7DaysAsync(
        Employee employee,
        CancellationToken cancellationToken)
    {
        var sevenDaysAgo = time.GetUtcNow().DateTime.AddDays(-7);

        var query = context.Set<WorkOrder>()
            .Where(wo => wo.Creator == employee)
            .Where(wo => (wo.AssignedDate >= sevenDaysAgo && wo.Status != WorkOrderStatus.Draft) ||
                        (wo.CompletedDate >= sevenDaysAgo && wo.Status == WorkOrderStatus.Complete))
            .OrderByDescending(wo => wo.AssignedDate ?? wo.CompletedDate);

        return await query.ToArrayAsync(cancellationToken);
    }
}
