using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

/// <summary>Seed steps for employee workload handler tests. Not a fixture.</summary>
internal static class EmployeeWorkLoadSteps
{
    /// <summary>
    /// Builds open, overdue, completed, cancelled, and unassigned orders for workload count assertions.
    /// </summary>
    public static WorkOrder[] BuildWorkloadOrders(Employee creator, Employee worker, DateOnly today) =>
    [
        WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).AssignedTo(worker).DueOn(today.AddDays(5)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).AssignedTo(worker).DueOn(today.AddDays(-3)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).AssignedTo(worker).DueOn(today.AddDays(-1)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).AssignedTo(worker).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).AssignedTo(worker)
            .DueOn(today.AddDays(-10)).CompletedAt(DateTime.UtcNow.AddDays(-5)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).AssignedTo(worker)
            .DueOn(today.AddDays(-90)).CompletedAt(DateTime.UtcNow.AddDays(-60)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Cancelled).AssignedTo(worker).DueOn(today.AddDays(-2)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).DueOn(today.AddDays(-2)).Build()
    ];
}
