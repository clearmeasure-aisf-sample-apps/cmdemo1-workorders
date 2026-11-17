using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// <see cref="EmployeeWorkLoadSummaryQueryHandler"/> for the UI.Server Native AOT build: every employee who can fulfill
/// work orders, in the same name order, with loads counted over the <see cref="WorkOrderSnapshot"/>.
/// </summary>
public class AotEmployeeWorkLoadSummaryQueryHandler(DataContext context, TimeProvider time)
    : IRequestHandler<EmployeeWorkLoadSummaryQuery, EmployeeWorkLoadSummary[]>
{
    private readonly WorkOrderSnapshotSource _source = new(context, time);

    public async ValueTask<EmployeeWorkLoadSummary[]> Handle(EmployeeWorkLoadSummaryQuery request,
        CancellationToken cancellationToken = default)
    {
        var employees = await _source.EmployeesWithRolesAsync(cancellationToken);
        var snapshot = await _source.LoadAsync(cancellationToken);
        var assignedByEmployee = snapshot.All.Where(wo => wo.Assignee != null).ToLookup(wo => wo.Assignee!.Id);
        return employees
            .Where(employee => employee.CanFulfillWorkOrder())
            .OrderBy(employee => employee.LastName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(employee => employee.FirstName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(employee => employee.UserName, StringComparer.OrdinalIgnoreCase)
            .Select(employee => new EmployeeWorkLoadSummary
            {
                Employee = employee,
                Load = AotEmployeeWorkLoadQueryHandler.Load(snapshot, assignedByEmployee[employee.Id].ToList())
            })
            .ToArray();
    }
}
