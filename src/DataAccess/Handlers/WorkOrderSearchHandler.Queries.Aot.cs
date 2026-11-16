using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// McpServer Native AOT build only (<c>McpServerAot=true</c>), in place of WorkOrderSearchHandler.Queries.cs. The MCP
/// tools filter by assignee, creator, status, and priority only. EF 11's query precompiler evaluates an optional filter
/// such as <c>assignee == null || wo.Assignee == assignee</c> at build time and silently drops it, so this build reads
/// every work order with one precompiled query and filters in memory. That suits a church-sized work-order table;
/// revisit when EF precompiles optional filters.
/// </summary>
public partial class WorkOrderSearchHandler
{
    private async Task<WorkOrder[]> ByPeopleAndStatusAsync(WorkOrderSpecificationQuery specification,
        CancellationToken cancellationToken)
    {
        var ct = cancellationToken;
        var all = await context.Set<WorkOrder>().ToArrayAsync(ct);
        return all
            .Where(wo => specification.Assignee == null || wo.Assignee?.Id == specification.Assignee.Id)
            .Where(wo => specification.Creator == null || wo.Creator?.Id == specification.Creator.Id)
            .Where(wo => specification.Status == null || wo.Status == specification.Status)
            .ToArray();
    }

    private Task<WorkOrder[]> ByAllFiltersAsync(WorkOrderSpecificationQuery specification,
        CancellationToken cancellationToken)
    {
        _ = time; // only the full search reads the clock; this keeps the primary-constructor parameter used
        throw new NotSupportedException(
            "This Native AOT build searches work orders by assignee, creator, status, and priority only.");
    }
}