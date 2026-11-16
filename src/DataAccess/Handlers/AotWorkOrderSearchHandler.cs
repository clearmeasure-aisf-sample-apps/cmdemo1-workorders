using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Runs a <see cref="WorkOrderSpecificationQuery"/> in the McpServer Native AOT build (<c>McpServerAot=true</c>), in
/// place of <c>WorkOrderSearchHandler</c>. The MCP tools filter by assignee, creator, status, and priority only.
/// </summary>
/// <remarks>
/// EF 11's query precompiler cannot take <c>WorkOrderSearchHandler</c>'s composed query. Written as one fixed query, it
/// evaluates an optional filter such as <c>assignee == null || wo.Assignee == assignee</c> at build time and silently
/// drops it. So this handler reads every work order with one precompiled query and filters and orders in memory. That
/// suits a church-sized work-order table; revisit when EF precompiles optional filters.
/// </remarks>
public class AotWorkOrderSearchHandler(DataContext context)
    : IRequestHandler<WorkOrderSpecificationQuery, WorkOrder[]>
{
    private readonly DataContext context = context;

    public async ValueTask<WorkOrder[]> Handle(WorkOrderSpecificationQuery specification,
        CancellationToken cancellationToken = default)
    {
        if (!UsesOnlySupportedFilters(specification))
        {
            throw new NotSupportedException(
                "This Native AOT build searches work orders by assignee, creator, status, and priority only.");
        }

        var ct = cancellationToken;
        var all = await context.Set<WorkOrder>().ToArrayAsync(ct);
        var matches = all
            .Where(wo => specification.Assignee == null || wo.Assignee?.Id == specification.Assignee.Id)
            .Where(wo => specification.Creator == null || wo.Creator?.Id == specification.Creator.Id)
            .Where(wo => specification.Status == null || wo.Status == specification.Status)
            .Where(wo => specification.Priority == null || wo.Priority == specification.Priority);
        return Order(matches, specification);
    }

    private static bool UsesOnlySupportedFilters(WorkOrderSpecificationQuery specification) =>
        string.IsNullOrWhiteSpace(specification.Building) && !specification.NoBuilding
        && string.IsNullOrWhiteSpace(specification.Room) && !specification.NoRoom
        && specification.CompletedWithinDays == null && string.IsNullOrWhiteSpace(specification.SearchText)
        && !specification.OverdueOnly && !specification.DueThisWeekOnly && !specification.OpenOnly
        && !specification.WaitingOnVendorOnly;

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