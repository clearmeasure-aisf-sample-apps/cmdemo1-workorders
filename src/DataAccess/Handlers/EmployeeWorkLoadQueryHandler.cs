using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class EmployeeWorkLoadQueryHandler(DataContext context, TimeProvider timeProvider)
    : IRequestHandler<EmployeeWorkLoadQuery, EmployeeWorkLoad>
{
    public async Task<EmployeeWorkLoad> Handle(EmployeeWorkLoadQuery request, CancellationToken cancellationToken = default)
    {
        var employee = await context.Set<Employee>()
            .SingleAsync(emp => emp.UserName == request.EmployeeUserName, cancellationToken);

        var workOrders = await context.Set<WorkOrder>()
            .Where(wo => wo.Assignee != null && wo.Assignee.Id == employee.Id)
            .ToListAsync(cancellationToken);

        var today = ChurchTimeZone.Today(timeProvider);
        var thirtyDaysAgo = timeProvider.GetUtcNow().DateTime.AddDays(-30);

        var open = workOrders.Where(IsOpen).ToList();
        var load = new EmployeeWorkLoad
        {
            AssignedAndOpen = open.Count,
            InProgress = open.Count(wo => wo.Status == WorkOrderStatus.InProgress),
            Overdue = open.Count(wo => wo.DueDate < today),
            CompletedLast30Days = workOrders.Count(wo => IsCompletedSince(wo, thirtyDaysAgo))
        };

        return load;
    }

    private static bool IsOpen(WorkOrder workOrder)
    {
        return workOrder.Status == WorkOrderStatus.Draft ||
               workOrder.Status == WorkOrderStatus.Assigned ||
               workOrder.Status == WorkOrderStatus.InProgress;
    }

    private static bool IsCompletedSince(WorkOrder workOrder, DateTime since)
    {
        return workOrder.Status == WorkOrderStatus.Complete && workOrder.CompletedDate >= since;
    }
}
