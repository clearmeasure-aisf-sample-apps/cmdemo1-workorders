using System.Linq.Expressions;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// EF-translatable projections that turn a work order into 1/0 flags and grouping keys,
/// built from <see cref="WorkOrderSpecifications"/> so counts match the filters.
/// </summary>
internal static class WorkOrderProjection
{
    /// <summary>
    /// The building a work order is grouped under: trimmed by the database exactly as the <c>Building</c>
    /// filter trims it; null, empty and blank become "" (the <c>NoBuilding</c> filter).
    /// </summary>
    private static readonly Expression<Func<WorkOrder, string>> BuildingKey = wo => (wo.Building ?? "").Trim();

    /// <summary>
    /// The room a work order is grouped under: trimmed by the database exactly as the <c>Room</c>
    /// filter trims it; null, empty and blank become "" (the <c>NoRoom</c> filter).
    /// </summary>
    private static readonly Expression<Func<WorkOrder, string>> RoomKey = wo => (wo.RoomNumber ?? "").Trim();

    /// <summary>
    /// Projects a work order to its building and room keys and 1/0 flags for the open, overdue and
    /// completed-within predicates.
    /// </summary>
    internal static Expression<Func<WorkOrder, WorkOrderCountRow>> CountRow(
        DateOnly today, TimeProvider time, int completedDays)
    {
        var wo = Expression.Parameter(typeof(WorkOrder), "wo");
        Expression<Func<WorkOrder, string?>> number = o => o.Number;
        var body = Expression.MemberInit(
            Expression.New(typeof(WorkOrderCountRow)),
            Bind(nameof(WorkOrderCountRow.Number), WorkOrderExpression.Rebind(number, wo)),
            Bind(nameof(WorkOrderCountRow.Building), WorkOrderExpression.Rebind(BuildingKey, wo)),
            Bind(nameof(WorkOrderCountRow.Room), WorkOrderExpression.Rebind(RoomKey, wo)),
            Bind(nameof(WorkOrderCountRow.Open), OneOrZero(WorkOrderExpression.Rebind(WorkOrderSpecifications.Open, wo))),
            Bind(nameof(WorkOrderCountRow.Overdue),
                OneOrZero(WorkOrderExpression.Rebind(WorkOrderSpecifications.Overdue(today), wo))),
            Bind(nameof(WorkOrderCountRow.Completed),
                OneOrZero(WorkOrderExpression.Rebind(WorkOrderSpecifications.CompletedWithin(completedDays, time), wo))));
        return Expression.Lambda<Func<WorkOrder, WorkOrderCountRow>>(body, wo);
    }

    /// <summary>
    /// Projects an assigned work order to its assignee and 1/0 flags for the Teams &amp; Vendors load
    /// counts. The caller drops work orders with no assignee.
    /// </summary>
    internal static Expression<Func<WorkOrder, AssigneeLoadFlag>> AssigneeLoadFlags(
        DateOnly today, TimeProvider time, int completedDays)
    {
        var wo = Expression.Parameter(typeof(WorkOrder), "wo");
        var inProgress = WorkOrderStatus.InProgress;
        Expression<Func<WorkOrder, Guid>> assigneeId = order => order.Assignee!.Id;
        Expression<Func<WorkOrder, bool>> isInProgress = order => order.Status == inProgress;
        var body = Expression.MemberInit(
            Expression.New(typeof(AssigneeLoadFlag)),
            BindFlag(nameof(AssigneeLoadFlag.AssigneeId), WorkOrderExpression.Rebind(assigneeId, wo)),
            BindFlag(nameof(AssigneeLoadFlag.Open), OneOrZero(WorkOrderExpression.Rebind(WorkOrderSpecifications.Open, wo))),
            BindFlag(nameof(AssigneeLoadFlag.InProgress), OneOrZero(WorkOrderExpression.Rebind(isInProgress, wo))),
            BindFlag(nameof(AssigneeLoadFlag.Overdue),
                OneOrZero(WorkOrderExpression.Rebind(WorkOrderSpecifications.Overdue(today), wo))),
            BindFlag(nameof(AssigneeLoadFlag.Completed),
                OneOrZero(WorkOrderExpression.Rebind(WorkOrderSpecifications.CompletedWithin(completedDays, time), wo))));
        return Expression.Lambda<Func<WorkOrder, AssigneeLoadFlag>>(body, wo);
    }

    private static MemberAssignment Bind(string member, Expression value) =>
        Expression.Bind(typeof(WorkOrderCountRow).GetProperty(member)!, value);

    private static MemberAssignment BindFlag(string member, Expression value) =>
        Expression.Bind(typeof(AssigneeLoadFlag).GetProperty(member)!, value);

    private static ConditionalExpression OneOrZero(Expression predicate) =>
        Expression.Condition(predicate, Expression.Constant(1), Expression.Constant(0));
}

/// <summary>
/// One work order reduced to its building and room keys and 1/0 flags, the input of the database-side
/// grouping of <see cref="BuildingRoomSummaryQueryHandler"/>.
/// </summary>
// ReSharper disable PropertyCanBeMadeInitOnly.Global -- bound by an expression-tree MemberInit translated by EF Core
internal sealed class WorkOrderCountRow
{
    public string? Number { get; set; }
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global -- setter required: assigned by Expression.Bind in CountRow
    public string Building { get; set; } = "";
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global -- setter required: assigned by Expression.Bind in CountRow
    public string Room { get; set; } = "";
    public int Open { get; set; }
    public int Overdue { get; set; }
    public int Completed { get; set; }
}

/// <summary>
/// One assigned work order reduced to its assignee and 1/0 load flags, the input of the grouping in
/// <see cref="EmployeeWorkLoadSummaryQueryHandler"/>.
/// </summary>
// ReSharper disable PropertyCanBeMadeInitOnly.Global -- bound by an expression-tree MemberInit translated by EF Core
internal sealed class AssigneeLoadFlag
{
    public Guid AssigneeId { get; set; }
    public int Open { get; set; }
    public int InProgress { get; set; }
    public int Overdue { get; set; }
    public int Completed { get; set; }
}
