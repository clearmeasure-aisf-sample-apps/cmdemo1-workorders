using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Handlers;

/// <summary>Seed steps for employee workload summary handler tests. Not a fixture.</summary>
internal static class EmployeeWorkLoadSummarySteps
{
    /// <summary>Creator, assignable worker, and another fulfiller used by the summary SQL seed.</summary>
    internal sealed record People(Employee Creator, Employee Worker, Employee Other);

    /// <summary>Builds the assignable / non-assignable work-order set used by the summary SQL test.</summary>
    public static WorkOrder[] AssignableOrders(People people, DateOnly today, DateTime since) =>
    [
        WorkOrderBuilder.For(people.Creator, WorkOrderStatus.Draft).WithNumber("WO-1").AssignedTo(people.Worker)
            .DueOn(today.AddDays(5)).Build(),
        WorkOrderBuilder.For(people.Creator, WorkOrderStatus.Assigned).WithNumber("WO-2").AssignedTo(people.Worker)
            .DueOn(today.AddDays(-3)).Build(),
        WorkOrderBuilder.For(people.Creator, WorkOrderStatus.InProgress).WithNumber("WO-3").AssignedTo(people.Worker)
            .DueOn(today.AddDays(-1)).Build(),
        WorkOrderBuilder.For(people.Creator, WorkOrderStatus.InProgress).WithNumber("WO-4").AssignedTo(people.Worker).Build(),
        WorkOrderBuilder.For(people.Creator, WorkOrderStatus.Complete).WithNumber("WO-5").AssignedTo(people.Worker)
            .DueOn(today.AddDays(-10)).CompletedAt(since.AddDays(1)).Build(),
        WorkOrderBuilder.For(people.Creator, WorkOrderStatus.Complete).WithNumber("WO-6").AssignedTo(people.Worker)
            .DueOn(today.AddDays(-90)).CompletedAt(since.AddSeconds(-1)).Build(),
        WorkOrderBuilder.For(people.Creator, WorkOrderStatus.Cancelled).WithNumber("WO-7").AssignedTo(people.Worker)
            .DueOn(today.AddDays(-2)).Build(),
        WorkOrderBuilder.For(people.Creator, WorkOrderStatus.Draft).WithNumber("WO-8").AssignedTo(people.Worker)
            .DueOn(today).Build(),
        WorkOrderBuilder.For(people.Creator, WorkOrderStatus.Complete).WithNumber("WO-9").AssignedTo(people.Worker)
            .DueOn(today).CompletedAt(since).Build(),
        WorkOrderBuilder.For(people.Creator, WorkOrderStatus.Assigned).WithNumber("WO-10").DueOn(today.AddDays(-2)).Build(),
        WorkOrderBuilder.For(people.Creator, WorkOrderStatus.Assigned).WithNumber("WO-11").AssignedTo(people.Creator)
            .DueOn(today.AddDays(-4)).Build(),
        WorkOrderBuilder.For(people.Creator, WorkOrderStatus.InProgress).WithNumber("WO-12").AssignedTo(people.Other)
            .DueOn(today.AddDays(-1)).Build()
    ];
}
