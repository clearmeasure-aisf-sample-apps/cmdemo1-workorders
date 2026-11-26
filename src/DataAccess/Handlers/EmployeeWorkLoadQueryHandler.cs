using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Load counts for one employee, using the same SQL-side flag projection as
/// <see cref="EmployeeWorkLoadSummaryQueryHandler"/>.
/// </summary>
public class EmployeeWorkLoadQueryHandler(DataContext context, TimeProvider timeProvider,
    ChurchCalendar calendar)
    : IRequestHandler<EmployeeWorkLoadQuery, EmployeeWorkLoad>
{
    private const int CompletedWindowDays = 30;

    public async Task<EmployeeWorkLoad> Handle(EmployeeWorkLoadQuery request, CancellationToken cancellationToken = default)
    {
        var employee = await context.Set<Employee>()
            .SingleAsync(emp => emp.UserName == request.EmployeeUserName, cancellationToken);

        var today = calendar.Today;
        var totals = await context.Set<WorkOrder>()
            .Where(wo => wo.Assignee != null && wo.Assignee.Id == employee.Id)
            .Select(WorkOrderProjection.AssigneeLoadFlags(today, timeProvider, CompletedWindowDays))
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Open = group.Sum(flag => flag.Open),
                InProgress = group.Sum(flag => flag.InProgress),
                Overdue = group.Sum(flag => flag.Overdue),
                Completed = group.Sum(flag => flag.Completed)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new EmployeeWorkLoad
        {
            AssignedAndOpen = totals?.Open ?? 0,
            InProgress = totals?.InProgress ?? 0,
            Overdue = totals?.Overdue ?? 0,
            CompletedLast30Days = totals?.Completed ?? 0
        };
    }
}
