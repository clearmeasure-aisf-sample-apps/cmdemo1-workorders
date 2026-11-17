using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;
/// <summary>
/// <see cref="EmployeeWorkLoadQueryHandler"/> for the UI.Server Native AOT build, counted over the
/// <see cref="WorkOrderSnapshot"/> with the same predicates.
/// </summary>
public class AotEmployeeWorkLoadQueryHandler(DataContext context, TimeProvider time)
    : IRequestHandler<EmployeeWorkLoadQuery, EmployeeWorkLoad>
{
    private readonly WorkOrderSnapshotSource _source = new(context, time);

    private const int CompletedWindowDays = 30;

    public async ValueTask<EmployeeWorkLoad> Handle(EmployeeWorkLoadQuery request, CancellationToken cancellationToken = default)
    {
        var employee = await _source.FindEmployeeAsync(request.EmployeeUserName, cancellationToken)
                       ?? throw new InvalidOperationException($"No employee named {request.EmployeeUserName}.");
        var snapshot = await _source.LoadAsync(cancellationToken);
        return Load(snapshot, snapshot.AssignedTo(employee.Id).ToList());
    }

    internal static EmployeeWorkLoad Load(WorkOrderSnapshot snapshot, IReadOnlyCollection<WorkOrder> assigned)
    {
        var completed = snapshot.IsCompletedWithin(CompletedWindowDays);
        return new EmployeeWorkLoad
        {
            AssignedAndOpen = assigned.Count(WorkOrderSnapshot.IsOpen),
            InProgress = assigned.Count(wo => wo.Status == WorkOrderStatus.InProgress),
            Overdue = assigned.Count(snapshot.IsOverdue),
            CompletedLast30Days = assigned.Count(completed)
        };
    }
}
