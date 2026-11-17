using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Runs a <see cref="WorkOrderSpecificationQuery"/> over a <see cref="WorkOrderSnapshot"/> in memory, with every filter
/// of <see cref="WorkOrderQueryFilters"/> and the same orders as <c>WorkOrderSearchHandler</c>. The Native AOT builds
/// answer the query this way (<c>AotWorkOrderSearchHandler</c>); the JIT build compiles it too so unit tests pin
/// its filters.
/// </summary>
internal static class WorkOrderSnapshotSearch
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    public static WorkOrder[] Run(WorkOrderSnapshot snapshot, WorkOrderSpecificationQuery specification) =>
        Order(snapshot.All.Where(Matches(specification, snapshot)), specification);

    private static Func<WorkOrder, bool> Matches(WorkOrderSpecificationQuery specification, WorkOrderSnapshot snapshot)
    {
        var filters = new List<Func<WorkOrder, bool>>();
        Add(filters, specification.Assignee, assignee => wo => wo.Assignee?.Id == assignee.Id);
        Add(filters, specification.Creator, creator => wo => wo.Creator?.Id == creator.Id);
        Add(filters, specification.Status, status => wo => wo.Status == status);
        AddValue(filters, specification.Priority, priority => wo => wo.Priority == priority);
        AddWhen(filters, specification.OverdueOnly, snapshot.IsOverdue);
        AddWhen(filters, specification.DueThisWeekOnly, snapshot.IsDueThisWeek);
        AddWhen(filters, specification.OpenOnly, WorkOrderSnapshot.IsOpen);
        AddWhen(filters, specification.WaitingOnVendorOnly,
            wo => wo.Status == WorkOrderStatus.InProgress && wo.WaitingOnVendorSince != null);
        filters.Add(NameFilter(specification.Building, specification.NoBuilding, wo => wo.Building));
        filters.Add(NameFilter(specification.Room, specification.NoRoom, wo => wo.RoomNumber));
        AddValue(filters, specification.CompletedWithinDays, snapshot.IsCompletedWithin);
        Add(filters, Blank(specification.SearchText),
            text => WorkOrderSearchText.Matches(text.Trim().ToLowerInvariant()).Compile());

        return wo => filters.TrueForAll(filter => filter(wo));
    }

    private static string? Blank(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;

    private static void Add<T>(List<Func<WorkOrder, bool>> filters, T? value, Func<T, Func<WorkOrder, bool>> filter)
        where T : class
    {
        if (value != null)
        {
            filters.Add(filter(value));
        }
    }

    private static void AddValue<T>(List<Func<WorkOrder, bool>> filters, T? value, Func<T, Func<WorkOrder, bool>> filter)
        where T : struct
    {
        if (value is { } present)
        {
            filters.Add(filter(present));
        }
    }

    private static void AddWhen(List<Func<WorkOrder, bool>> filters, bool when, Func<WorkOrder, bool> filter)
    {
        if (when)
        {
            filters.Add(filter);
        }
    }

    /// <summary>A trimmed name equal to <paramref name="name"/> ignoring case, or blank when <paramref name="none"/>.</summary>
    private static Func<WorkOrder, bool> NameFilter(string? name, bool none, Func<WorkOrder, string?> select)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var trimmed = name.Trim();
            return wo => select(wo) is { } value && KeyComparer.Equals(value.Trim(), trimmed);
        }

        return none ? wo => string.IsNullOrWhiteSpace(select(wo)) : _ => true;
    }

    private static WorkOrder[] Order(IEnumerable<WorkOrder> matches, WorkOrderSpecificationQuery specification)
    {
        if (!specification.OrderByDueDate)
        {
            return matches.OrderByDescending(wo => wo.CreatedDate).ThenBy(wo => wo.Number, StringComparer.Ordinal).ToArray();
        }

        var nullsLast = matches.OrderBy(wo => wo.DueDate == null);
        var byDueDate = specification.DueDateDescending
            ? nullsLast.ThenByDescending(wo => wo.DueDate)
            : nullsLast.ThenBy(wo => wo.DueDate);
        return byDueDate.ThenBy(wo => wo.Number, StringComparer.Ordinal).ToArray();
    }
}
