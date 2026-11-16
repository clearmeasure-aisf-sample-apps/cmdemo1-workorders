using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// The search queries for the JIT hosts. Not in the McpServer Native AOT build, which uses
/// WorkOrderSearchHandler.Queries.Aot.cs: EF 11's query precompiler evaluates a test on local values alone (such as
/// <c>assignee == null</c>) at build time and silently drops the filter, cannot quote the full query ("Argument types do
/// not match"), and the full query's SQL, built at run time, fails under Native AOT for the converted Status and Priority
/// columns ("Model building is not supported").
/// </summary>
public partial class WorkOrderSearchHandler
{
    private async Task<WorkOrder[]> ByPeopleAndStatusAsync(WorkOrderSpecificationQuery specification,
        CancellationToken cancellationToken)
    {
        var assignee = specification.Assignee;
        var creator = specification.Creator;
        var status = specification.Status;
        return await context.Set<WorkOrder>()
            .Where(wo => assignee == null || wo.Assignee == assignee)
            .Where(wo => creator == null || wo.Creator == creator)
            .Where(wo => status == null || wo.Status == status)
            .ToArrayAsync(cancellationToken);
    }

    private async Task<WorkOrder[]> ByAllFiltersAsync(WorkOrderSpecificationQuery specification,
        CancellationToken cancellationToken)
    {
        var today = ChurchTimeZone.Today(time);
        var weekEnd = today.AddDays(6);
        var draft = WorkOrderStatus.Draft;
        var assigned = WorkOrderStatus.Assigned;
        var inProgress = WorkOrderStatus.InProgress;
        var complete = WorkOrderStatus.Complete;
        var assignee = specification.Assignee;
        var creator = specification.Creator;
        var status = specification.Status;
        var building = Trimmed(specification.Building);
        var room = Trimmed(specification.Room)?.ToLower();
        var completedSince = specification.CompletedWithinDays is { } days
            ? time.GetUtcNow().DateTime.AddDays(-days)
            : (DateTime?)null;
        var completedSinceValue = completedSince ?? default;
        var text = Trimmed(specification.SearchText)?.ToLower();
        var pattern = text == null ? null : "%" + EscapeLike(text) + "%";
        var overdueOnly = specification.OverdueOnly;
        var dueThisWeekOnly = specification.DueThisWeekOnly;
        var openOnly = specification.OpenOnly;
        var waitingOnVendorOnly = specification.WaitingOnVendorOnly;
        var noBuilding = building == null && specification.NoBuilding;
        var noRoom = room == null && specification.NoRoom;
        var ct = cancellationToken;

        return await context.Set<WorkOrder>()
            .Where(wo => assignee == null || wo.Assignee == assignee)
            .Where(wo => creator == null || wo.Creator == creator)
            .Where(wo => status == null || wo.Status == status)
            .Where(wo => !overdueOnly
                || ((wo.Status == draft || wo.Status == assigned || wo.Status == inProgress)
                    && wo.DueDate != null && wo.DueDate.Value < today))
            .Where(wo => !dueThisWeekOnly
                || ((wo.Status == draft || wo.Status == assigned || wo.Status == inProgress)
                    && wo.DueDate != null && wo.DueDate.Value >= today && wo.DueDate.Value <= weekEnd))
            .Where(wo => !openOnly || wo.Status == draft || wo.Status == assigned || wo.Status == inProgress)
            .Where(wo => !waitingOnVendorOnly || (wo.Status == inProgress && wo.WaitingOnVendorSince != null))
            .Where(wo => building == null || (wo.Building != null && wo.Building.Trim() == building))
            .Where(wo => !noBuilding || wo.Building == null || wo.Building.Trim() == "")
            .Where(wo => room == null || (wo.RoomNumber != null && wo.RoomNumber.Trim().ToLower() == room))
            .Where(wo => !noRoom || wo.RoomNumber == null || wo.RoomNumber.Trim() == "")
            .Where(wo => completedSince == null
                || (wo.Status == complete && wo.CompletedDate != null && wo.CompletedDate.Value >= completedSinceValue))
            .Where(wo => text == null
                || (wo.Number != null && EF.Functions.Like(wo.Number.ToLower(), pattern))
                || (wo.Title != null && EF.Functions.Like(wo.Title.ToLower(), pattern))
                || (wo.RoomNumber != null && EF.Functions.Like(wo.RoomNumber.ToLower(), pattern))
                || EF.Functions.Like(wo.Creator!.FirstName.ToLower(), pattern)
                || EF.Functions.Like(wo.Creator!.LastName.ToLower(), pattern)
                || EF.Functions.Like(wo.Assignee!.FirstName.ToLower(), pattern)
                || EF.Functions.Like(wo.Assignee!.LastName.ToLower(), pattern))
            .ToArrayAsync(ct);
    }

    // SQL Server LIKE wildcards are escaped with brackets, so the pattern needs no ESCAPE clause.
    private static string EscapeLike(string value) =>
        value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}