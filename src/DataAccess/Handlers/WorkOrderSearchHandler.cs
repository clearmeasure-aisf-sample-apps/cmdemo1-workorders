using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Runs a <see cref="WorkOrderSpecificationQuery"/>. The default order is newest first, then by number.
/// Due-date order, when requested, keeps work orders with no due date last.
/// </summary>
/// <remarks>
/// The queries live in WorkOrderSearchHandler.Queries.cs; the McpServer Native AOT build uses
/// WorkOrderSearchHandler.Queries.Aot.cs instead. Matches are ordered in memory; the search returns every match, so
/// there is no paging to push down.
/// </remarks>
public partial class WorkOrderSearchHandler(DataContext context, TimeProvider time)
    : IRequestHandler<WorkOrderSpecificationQuery, WorkOrder[]>
{
    private readonly DataContext context = context;

    public async ValueTask<WorkOrder[]> Handle(WorkOrderSpecificationQuery specification,
        CancellationToken cancellationToken = default)
    {
        var matches = UsesOnlyPeopleAndStatus(specification)
            ? await ByPeopleAndStatusAsync(specification, cancellationToken)
            : await ByAllFiltersAsync(specification, cancellationToken);

        // Priority is filtered here, not in SQL: comparing the enum column makes EF look up an int converter for it at
        // run time, which Native AOT cannot create. The search returns every match, so no extra rows cross a page.
        var priority = specification.Priority;
        var byPriority = priority == null ? matches : matches.Where(wo => wo.Priority == priority);
        return Order(byPriority, specification);
    }

    private static bool UsesOnlyPeopleAndStatus(WorkOrderSpecificationQuery specification) =>
        Trimmed(specification.Building) == null && !specification.NoBuilding
        && Trimmed(specification.Room) == null && !specification.NoRoom
        && specification.CompletedWithinDays == null && Trimmed(specification.SearchText) == null
        && !specification.OverdueOnly && !specification.DueThisWeekOnly && !specification.OpenOnly
        && !specification.WaitingOnVendorOnly;

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
