using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Runs a <see cref="WorkOrderSpecificationQuery"/> in the Native AOT builds (<c>McpServerAot=true</c> or
/// <c>UiServerAot=true</c>), in place of <c>WorkOrderSearchHandler</c>, with every filter of
/// <see cref="WorkOrderQueryFilters"/> applied to the <see cref="WorkOrderSnapshot"/>.
/// </summary>
/// <remarks>
/// EF 11's query precompiler cannot take <c>WorkOrderSearchHandler</c>'s composed query. Written as one fixed query, it
/// evaluates an optional filter such as <c>assignee == null || wo.Assignee == assignee</c> at build time and silently
/// drops it. So this handler filters and orders the snapshot in memory.
/// </remarks>
public class AotWorkOrderSearchHandler(DataContext context, TimeProvider time)
    : IRequestHandler<WorkOrderSpecificationQuery, WorkOrder[]>
{
    private readonly WorkOrderSnapshotSource _source = new(context, time);

    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    public async ValueTask<WorkOrder[]> Handle(WorkOrderSpecificationQuery specification,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _source.LoadAsync(cancellationToken);
        return Order(snapshot.All.Where(Matches(specification, snapshot)), specification);
    }

    private static Func<WorkOrder, bool> Matches(WorkOrderSpecificationQuery specification, WorkOrderSnapshot snapshot)
    {
        var filters = new List<Func<WorkOrder, bool>>();
        Add(filters, specification.Assignee, assignee => wo => wo.Assignee?.Id == assignee.Id);
        Add(filters, specification.Creator, creator => wo => wo.Creator?.Id == creator.Id);
        Add(filters, specification.Status, status => wo => wo.Status == status);
        Add(filters, specification.Priority, priority => wo => wo.Priority == priority);
        AddWhen(filters, specification.OverdueOnly, snapshot.IsOverdue);
        AddWhen(filters, specification.DueThisWeekOnly, snapshot.IsDueThisWeek);
        AddWhen(filters, specification.OpenOnly, WorkOrderSnapshot.IsOpen);
        AddWhen(filters, specification.WaitingOnVendorOnly,
            wo => wo.Status == WorkOrderStatus.InProgress && wo.WaitingOnVendorSince != null);
        filters.Add(NameFilter(specification.Building, specification.NoBuilding, wo => wo.Building));
        filters.Add(NameFilter(specification.Room, specification.NoRoom, wo => wo.RoomNumber));
        if (specification.CompletedWithinDays is { } days)
        {
            filters.Add(snapshot.IsCompletedWithin(days));
        }

        if (!string.IsNullOrWhiteSpace(specification.SearchText))
        {
            filters.Add(WorkOrderSearchText.Matches(specification.SearchText.Trim().ToLower()).Compile());
        }

        return wo => filters.TrueForAll(filter => filter(wo));
    }

    private static void Add<T>(List<Func<WorkOrder, bool>> filters, T? value, Func<T, Func<WorkOrder, bool>> filter)
    {
        if (value != null)
        {
            filters.Add(filter(value));
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
