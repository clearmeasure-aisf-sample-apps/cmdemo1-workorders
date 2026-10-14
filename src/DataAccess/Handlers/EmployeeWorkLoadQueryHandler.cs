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

        var load = new EmployeeWorkLoad
        {
            AssignedAndOpen = workOrders.Count(wo =>
                wo.Status == WorkOrderStatus.Draft ||
                wo.Status == WorkOrderStatus.Assigned ||
                wo.Status == WorkOrderStatus.InProgress),
            InProgress = workOrders.Count(wo => wo.Status == WorkOrderStatus.InProgress),
            Overdue = workOrders.Count(wo =>
                (wo.Status == WorkOrderStatus.Draft ||
                 wo.Status == WorkOrderStatus.Assigned ||
                 wo.Status == WorkOrderStatus.InProgress) &&
                wo.DueDate < today),
            CompletedLast30Days = workOrders.Count(wo =>
                wo.Status == WorkOrderStatus.Complete &&
                wo.CompletedDate >= thirtyDaysAgo)
        };

        return load;
    }
}
