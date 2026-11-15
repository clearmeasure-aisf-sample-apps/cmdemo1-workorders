using System.Linq.Expressions;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Named work-order predicates shared by search, summary, reports and in-memory callers.
/// One definition for SQL translation and compiled in-memory matching.
/// </summary>
public static class WorkOrderSpecifications
{
    /// <summary>Open work orders: Draft, Assigned or In Progress.</summary>
    public static Expression<Func<WorkOrder, bool>> Open { get; } = CreateOpen();

    /// <summary>True when <paramref name="workOrder"/> satisfies <see cref="Open"/>.</summary>
    public static bool IsOpen(WorkOrder workOrder) => IsOpen(workOrder.Status);

    /// <summary>True when <paramref name="status"/> is Draft, Assigned or In Progress (same as <see cref="Open"/>).</summary>
    public static bool IsOpen(WorkOrderStatus status) =>
        status == WorkOrderStatus.Draft
        || status == WorkOrderStatus.Assigned
        || status == WorkOrderStatus.InProgress;

    /// <summary>Open work orders whose due date is before <paramref name="today"/>.</summary>
    public static Expression<Func<WorkOrder, bool>> Overdue(DateOnly today) =>
        WorkOrderExpression.AndAlso(Open, wo => wo.DueDate < today);

    /// <summary>Open work orders due from <paramref name="today"/> through the next six days.</summary>
    public static Expression<Func<WorkOrder, bool>> DueThisWeek(DateOnly today)
    {
        var lastDay = today.AddDays(6);
        return WorkOrderExpression.AndAlso(Open, wo => wo.DueDate >= today && wo.DueDate <= lastDay);
    }

    /// <summary>Completed work orders whose completion is within the last <paramref name="days"/> days.</summary>
    public static Expression<Func<WorkOrder, bool>> CompletedWithin(int days, TimeProvider time)
    {
        var complete = WorkOrderStatus.Complete;
        var since = time.GetUtcNow().DateTime.AddDays(-days);
        return wo => wo.Status == complete && wo.CompletedDate >= since;
    }

    private static Expression<Func<WorkOrder, bool>> CreateOpen()
    {
        var draft = WorkOrderStatus.Draft;
        var assigned = WorkOrderStatus.Assigned;
        var inProgress = WorkOrderStatus.InProgress;
        return wo => wo.Status == draft || wo.Status == assigned || wo.Status == inProgress;
    }
}
