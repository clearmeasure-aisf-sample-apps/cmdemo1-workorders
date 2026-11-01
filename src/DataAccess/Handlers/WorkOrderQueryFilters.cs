using System.Linq.Expressions;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Composable work-order filters shared by the search handlers. Every filter is a small method so
/// the chain stays readable and each predicate translates on SQL Server, SQLite and in memory.
/// "Open" means Draft, Assigned or In Progress; "today" is the church's calendar date.
/// </summary>
internal static class WorkOrderQueryFilters
{
    public static IQueryable<WorkOrder> Apply(
        IQueryable<WorkOrder> query,
        Employee? assignee,
        Employee? creator,
        WorkOrderStatus? status,
        bool overdueOnly = false) =>
        ApplyOverdue(ApplyBasics(query, assignee, creator, status), overdueOnly, ChurchTimeZone.Today(TimeProvider.System));

    public static IQueryable<WorkOrder> Apply(
        IQueryable<WorkOrder> query,
        WorkOrderSearchSpecification specification) =>
        ApplyBasics(query, specification.Assignee, specification.Creator, specification.Status);

    public static IQueryable<WorkOrder> Apply(
        IQueryable<WorkOrder> query,
        WorkOrderSpecificationQuery specification,
        TimeProvider time)
    {
        query = ApplyBasics(query, specification.Assignee, specification.Creator, specification.Status);
        query = ApplyOverdue(query, specification.OverdueOnly, ChurchTimeZone.Today(time));
        query = ApplyDueThisWeek(query, specification.DueThisWeekOnly, ChurchTimeZone.Today(time));
        query = ApplyPriority(query, specification.Priority);
        query = ApplyBuilding(query, specification.Building, specification.NoBuilding);
        query = ApplyRoom(query, specification.Room, specification.NoRoom);
        query = ApplyOpen(query, specification.OpenOnly);
        query = ApplyWaitingOnVendor(query, specification.WaitingOnVendorOnly);
        query = ApplyCompletedWithin(query, specification.CompletedWithinDays, time);
        return ApplySearchText(query, specification.SearchText);
    }

    /// <summary>Open work orders: Draft, Assigned or In Progress.</summary>
    public static IQueryable<WorkOrder> OpenOnly(IQueryable<WorkOrder> query) => query.Where(IsOpen());

    /// <summary>Open work orders whose due date is before <paramref name="today"/>.</summary>
    public static IQueryable<WorkOrder> Overdue(IQueryable<WorkOrder> query, DateOnly today) =>
        query.Where(IsOverdue(today));

    /// <summary>Open work orders due from <paramref name="today"/> through the next six days.</summary>
    public static IQueryable<WorkOrder> DueThisWeek(IQueryable<WorkOrder> query, DateOnly today) =>
        query.Where(IsDueThisWeek(today));

    /// <summary>Completed work orders whose completion is within the last <paramref name="days"/> days.</summary>
    public static IQueryable<WorkOrder> CompletedWithin(IQueryable<WorkOrder> query, int days, TimeProvider time) =>
        query.Where(IsCompletedWithin(days, time));

    /// <summary>Predicate of an open work order: Draft, Assigned or In Progress.</summary>
    public static Expression<Func<WorkOrder, bool>> IsOpen()
    {
        var draft = WorkOrderStatus.Draft;
        var assigned = WorkOrderStatus.Assigned;
        var inProgress = WorkOrderStatus.InProgress;
        return wo => wo.Status == draft || wo.Status == assigned || wo.Status == inProgress;
    }

    /// <summary>Predicate of an open work order whose due date is before <paramref name="today"/>.</summary>
    public static Expression<Func<WorkOrder, bool>> IsOverdue(DateOnly today) =>
        AndAlso(IsOpen(), wo => wo.DueDate < today);

    /// <summary>Predicate of an open work order due from <paramref name="today"/> through the next six days.</summary>
    private static Expression<Func<WorkOrder, bool>> IsDueThisWeek(DateOnly today)
    {
        var lastDay = today.AddDays(6);
        return AndAlso(IsOpen(), wo => wo.DueDate >= today && wo.DueDate <= lastDay);
    }

    /// <summary>Predicate of a work order completed within the last <paramref name="days"/> days.</summary>
    public static Expression<Func<WorkOrder, bool>> IsCompletedWithin(int days, TimeProvider time)
    {
        var complete = WorkOrderStatus.Complete;
        var since = time.GetUtcNow().DateTime.AddDays(-days);
        return wo => wo.Status == complete && wo.CompletedDate >= since;
    }

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
    /// completed-within predicates, built from the same expressions as the filters (no copied predicate).
    /// </summary>
    internal static Expression<Func<WorkOrder, WorkOrderCountRow>> CountRow(DateOnly today, TimeProvider time, int completedDays)
    {
        var wo = Expression.Parameter(typeof(WorkOrder), "wo");
        Expression<Func<WorkOrder, string?>> number = o => o.Number;
        var body = Expression.MemberInit(
            Expression.New(typeof(WorkOrderCountRow)),
            Bind(nameof(WorkOrderCountRow.Number), Rebind(number, wo)),
            Bind(nameof(WorkOrderCountRow.Building), Rebind(BuildingKey, wo)),
            Bind(nameof(WorkOrderCountRow.Room), Rebind(RoomKey, wo)),
            Bind(nameof(WorkOrderCountRow.Open), OneOrZero(Rebind(IsOpen(), wo))),
            Bind(nameof(WorkOrderCountRow.Overdue), OneOrZero(Rebind(IsOverdue(today), wo))),
            Bind(nameof(WorkOrderCountRow.Completed), OneOrZero(Rebind(IsCompletedWithin(completedDays, time), wo))));
        return Expression.Lambda<Func<WorkOrder, WorkOrderCountRow>>(body, wo);
    }

    private static MemberAssignment Bind(string member, Expression value) =>
        Expression.Bind(typeof(WorkOrderCountRow).GetProperty(member)!, value);

    private static ConditionalExpression OneOrZero(Expression predicate) =>
        Expression.Condition(predicate, Expression.Constant(1), Expression.Constant(0));

    private static Expression Rebind<T>(Expression<Func<WorkOrder, T>> expression, ParameterExpression parameter) =>
        new ParameterReplacer(expression.Parameters[0], parameter).Visit(expression.Body);

    private static IQueryable<WorkOrder> ApplyBasics(
        IQueryable<WorkOrder> query,
        Employee? assignee,
        Employee? creator,
        WorkOrderStatus? status) =>
        ApplyStatus(ApplyCreator(ApplyAssignee(query, assignee), creator), status);

    private static IQueryable<WorkOrder> ApplyAssignee(IQueryable<WorkOrder> query, Employee? assignee) =>
        assignee == null ? query : query.Where(wo => wo.Assignee == assignee);

    private static IQueryable<WorkOrder> ApplyCreator(IQueryable<WorkOrder> query, Employee? creator) =>
        creator == null ? query : query.Where(wo => wo.Creator == creator);

    private static IQueryable<WorkOrder> ApplyStatus(IQueryable<WorkOrder> query, WorkOrderStatus? status) =>
        status == null ? query : query.Where(wo => wo.Status == status);

    private static IQueryable<WorkOrder> ApplyOverdue(IQueryable<WorkOrder> query, bool overdueOnly, DateOnly today) =>
        overdueOnly ? Overdue(query, today) : query;

    private static IQueryable<WorkOrder> ApplyDueThisWeek(IQueryable<WorkOrder> query, bool dueThisWeekOnly, DateOnly today) =>
        dueThisWeekOnly ? DueThisWeek(query, today) : query;

    private static IQueryable<WorkOrder> ApplyPriority(IQueryable<WorkOrder> query, WorkOrderPriority? priority) =>
        priority == null ? query : query.Where(wo => wo.Priority == priority);

    private static IQueryable<WorkOrder> ApplyBuilding(IQueryable<WorkOrder> query, string? building, bool noBuilding)
    {
        if (!string.IsNullOrWhiteSpace(building))
        {
            var trimmed = building.Trim();
            return query.Where(wo => wo.Building != null && wo.Building.Trim() == trimmed);
        }

        if (noBuilding)
        {
            return query.Where(wo => wo.Building == null || wo.Building.Trim() == "");
        }

        return query;
    }

    private static IQueryable<WorkOrder> ApplyRoom(IQueryable<WorkOrder> query, string? room, bool noRoom)
    {
        if (!string.IsNullOrWhiteSpace(room))
        {
            var trimmed = room.Trim().ToLower();
            return query.Where(wo => wo.RoomNumber != null && wo.RoomNumber.Trim().ToLower() == trimmed);
        }

        if (noRoom)
        {
            return query.Where(wo => wo.RoomNumber == null || wo.RoomNumber.Trim() == "");
        }

        return query;
    }

    private static IQueryable<WorkOrder> ApplyOpen(IQueryable<WorkOrder> query, bool openOnly) =>
        openOnly ? OpenOnly(query) : query;

    private static IQueryable<WorkOrder> ApplyWaitingOnVendor(IQueryable<WorkOrder> query, bool waitingOnVendorOnly)
    {
        var inProgress = WorkOrderStatus.InProgress;
        return waitingOnVendorOnly
            ? query.Where(wo => wo.Status == inProgress && wo.WaitingOnVendorSince != null)
            : query;
    }

    private static IQueryable<WorkOrder> ApplyCompletedWithin(IQueryable<WorkOrder> query, int? days, TimeProvider time) =>
        days == null ? query : CompletedWithin(query, days.Value, time);

    private static IQueryable<WorkOrder> ApplySearchText(IQueryable<WorkOrder> query, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return query;
        }

        var q = searchText.Trim().ToLower();
        return query.Where(OrElse(NumberTitleOrRoomContains(q), CreatorOrAssigneeNameContains(q)));
    }

    private static Expression<Func<WorkOrder, bool>> NumberTitleOrRoomContains(string q) =>
        wo => (wo.Number != null && wo.Number.ToLower().Contains(q)) ||
              (wo.Title != null && wo.Title.ToLower().Contains(q)) ||
              (wo.RoomNumber != null && wo.RoomNumber.ToLower().Contains(q));

    private static Expression<Func<WorkOrder, bool>> CreatorOrAssigneeNameContains(string q) =>
        wo => (wo.Creator != null && (wo.Creator.FirstName.ToLower().Contains(q) || wo.Creator.LastName.ToLower().Contains(q))) ||
              (wo.Assignee != null && (wo.Assignee.FirstName.ToLower().Contains(q) || wo.Assignee.LastName.ToLower().Contains(q)));

    private static Expression<Func<WorkOrder, bool>> OrElse(
        Expression<Func<WorkOrder, bool>> left,
        Expression<Func<WorkOrder, bool>> right) =>
        Combine(left, right, Expression.OrElse);

    private static Expression<Func<WorkOrder, bool>> AndAlso(
        Expression<Func<WorkOrder, bool>> left,
        Expression<Func<WorkOrder, bool>> right) =>
        Combine(left, right, Expression.AndAlso);

    private static Expression<Func<WorkOrder, bool>> Combine(
        Expression<Func<WorkOrder, bool>> left,
        Expression<Func<WorkOrder, bool>> right,
        Func<Expression, Expression, BinaryExpression> combine)
    {
        var parameter = left.Parameters[0];
        return Expression.Lambda<Func<WorkOrder, bool>>(combine(left.Body, Rebind(right, parameter)), parameter);
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
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
