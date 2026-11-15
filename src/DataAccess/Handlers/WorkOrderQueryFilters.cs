using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Composable work-order filters shared by the search handlers. Every filter is a small method so
/// the chain stays readable and each predicate translates on SQL Server, SQLite and in memory.
/// Domain predicates live in <see cref="WorkOrderSpecifications"/>.
/// </summary>
internal static class WorkOrderQueryFilters
{
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
    public static IQueryable<WorkOrder> OpenOnly(IQueryable<WorkOrder> query) =>
        query.Where(WorkOrderSpecifications.Open);

    /// <summary>Open work orders whose due date is before <paramref name="today"/>.</summary>
    public static IQueryable<WorkOrder> Overdue(IQueryable<WorkOrder> query, DateOnly today) =>
        query.Where(WorkOrderSpecifications.Overdue(today));

    /// <summary>Open work orders due from <paramref name="today"/> through the next six days.</summary>
    public static IQueryable<WorkOrder> DueThisWeek(IQueryable<WorkOrder> query, DateOnly today) =>
        query.Where(WorkOrderSpecifications.DueThisWeek(today));

    /// <summary>Completed work orders whose completion is within the last <paramref name="days"/> days.</summary>
    public static IQueryable<WorkOrder> CompletedWithin(IQueryable<WorkOrder> query, int days, TimeProvider time) =>
        query.Where(WorkOrderSpecifications.CompletedWithin(days, time));

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
        return query.Where(wo =>
            (wo.Number != null && wo.Number.ToLower().Contains(q)) ||
            (wo.Title != null && wo.Title.ToLower().Contains(q)) ||
            (wo.RoomNumber != null && wo.RoomNumber.ToLower().Contains(q)) ||
            (wo.Creator != null && (wo.Creator.FirstName.ToLower().Contains(q) || wo.Creator.LastName.ToLower().Contains(q))) ||
            (wo.Assignee != null && (wo.Assignee.FirstName.ToLower().Contains(q) || wo.Assignee.LastName.ToLower().Contains(q))));
    }
}
