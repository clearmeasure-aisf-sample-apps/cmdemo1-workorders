using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Runs a <see cref="WorkOrderSpecificationQuery"/>. The default order is newest first, then by number.
/// Due-date order, when requested, keeps work orders with no due date last.
/// </summary>
/// <remarks>
/// One fixed-shape query: every optional filter is a "not requested, or matches" term, so EF can precompile it for
/// Native AOT (a query composed at run time cannot be precompiled). Each input is copied to a local first because
/// EF 11's query precompiler cannot read method parameters (dotnet/efcore#35494). The name terms skip an explicit
/// "navigation != null" check: SQL's LEFT JOIN already yields no match, and the precompiler fails to quote that
/// null-key comparison ("Argument types do not match"). Matches are ordered in memory;
/// the search returns every match, so there is no paging to push down.
/// </remarks>
public class WorkOrderSearchHandler(DataContext context, TimeProvider time) : IRequestHandler<WorkOrderSpecificationQuery, WorkOrder[]>
{
    private readonly DataContext context = context;

    public async ValueTask<WorkOrder[]> Handle(WorkOrderSpecificationQuery specification,
        CancellationToken cancellationToken = default)
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
        var priority = specification.Priority;
        var overdueOnly = specification.OverdueOnly;
        var dueThisWeekOnly = specification.DueThisWeekOnly;
        var openOnly = specification.OpenOnly;
        var waitingOnVendorOnly = specification.WaitingOnVendorOnly;
        var building = Trimmed(specification.Building);
        var noBuilding = building == null && specification.NoBuilding;
        var room = Trimmed(specification.Room)?.ToLower();
        var noRoom = room == null && specification.NoRoom;
        var completedSince = specification.CompletedWithinDays is { } days
            ? time.GetUtcNow().DateTime.AddDays(-days)
            : (DateTime?)null;
        var completedSinceValue = completedSince ?? default;
        var text = Trimmed(specification.SearchText)?.ToLower();
        var pattern = text == null ? null : "%" + EscapeLike(text) + "%";
        var ct = cancellationToken;

        WorkOrder[] matches;
        if (building == null && !noBuilding && room == null && !noRoom && completedSince == null && text == null
            && !overdueOnly && !dueThisWeekOnly && !openOnly && !waitingOnVendorOnly)
        {
            matches = await ByPeopleAndStatusAsync(specification, ct);
        }
        else
        {
            matches = await context.Set<WorkOrder>()
                .Where(wo => assignee == null || wo.Assignee == assignee)
                .Where(wo => creator == null || wo.Creator == creator)
                .Where(wo => status == null || wo.Status == status)
                .Where(wo => !overdueOnly
                    || ((wo.Status == draft || wo.Status == assigned || wo.Status == inProgress) && wo.DueDate != null && wo.DueDate.Value < today))
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

        // Priority is filtered here, not in SQL: comparing the enum column makes EF look up an int converter for it at
        // run time, which Native AOT cannot create. The search returns every match, so no extra rows cross a page.
        var byPriority = priority == null ? matches : matches.Where(wo => wo.Priority == priority);
        return Order(byPriority, specification);
    }

    /// <summary>
    /// The common search (assignee, creator, status): at most three nullable SQL parameters, so EF pregenerates its SQL
    /// when precompiling for Native AOT. The full query has more, so EF 11 builds its SQL at run time, and under Native
    /// AOT that fails for the converted Status/Priority columns ("Model building is not supported").
    /// </summary>
    private async Task<WorkOrder[]> ByPeopleAndStatusAsync(WorkOrderSpecificationQuery specification,
        CancellationToken cancellationToken)
    {
        // Read members, not bare method parameters: EF 11's precompiler treats a local copied straight from a
        // parameter as a null constant, so these filters would be dropped from the SQL (dotnet/efcore#35494).
        var assignee = specification.Assignee;
        var creator = specification.Creator;
        var status = specification.Status;
        var ct = cancellationToken;
        return await context.Set<WorkOrder>()
            .Where(wo => assignee == null || wo.Assignee == assignee)
            .Where(wo => creator == null || wo.Creator == creator)
            .Where(wo => status == null || wo.Status == status)
            .ToArrayAsync(ct);
    }
    // SQL Server LIKE wildcards are escaped with brackets, so the pattern needs no ESCAPE clause.
    private static string EscapeLike(string value) =>
        value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

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
