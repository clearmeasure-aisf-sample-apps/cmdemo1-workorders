using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Loads every employee who can fulfill work orders, with open, in-progress, overdue and
/// recently completed counts, in one SQL statement. Work orders are reduced to 1/0 flags and
/// grouped by assignee; employees with no assigned work orders stay in the result with zeros.
/// The predicates match <see cref="EmployeeWorkLoadQueryHandler"/>.
/// </summary>
public class EmployeeWorkLoadSummaryQueryHandler(DataContext context, TimeProvider timeProvider)
    : IRequestHandler<EmployeeWorkLoadSummaryQuery, EmployeeWorkLoadSummary[]>
{
    private const int CompletedWindowDays = 30;

    public async Task<EmployeeWorkLoadSummary[]> Handle(EmployeeWorkLoadSummaryQuery request,
        CancellationToken cancellationToken = default)
    {
        var rows = await SummaryQuery().ToListAsync(cancellationToken);
        return EmployeeWorkLoadRow.ToSummaries(rows);
    }

    /// <summary>
    /// Assignable employees left-joined to assignee counts. One row per employee role; the counts
    /// are already grouped by assignee, so extra roles do not multiply them.
    /// </summary>
    internal IQueryable<EmployeeWorkLoadRow> SummaryQuery() =>
        from member in AssignableEmployeeRoles()
        join load in AssigneeCounts() on member.Id equals load.AssigneeId into loads
        from load in loads.DefaultIfEmpty()
        orderby member.LastName, member.FirstName, member.UserName, member.RoleName
        select new EmployeeWorkLoadRow
        {
            Member = member,
            // The cast is never null in C#. EF still needs it so a missing left join becomes COALESCE(..., 0).
            // ReSharper disable RedundantCast
            // ReSharper disable ConstantNullCoalescingCondition -- EF translates the coalesce so a missing left join becomes 0
            AssignedAndOpen = (int?)load.AssignedAndOpen ?? 0,
            InProgress = (int?)load.InProgress ?? 0,
            Overdue = (int?)load.Overdue ?? 0,
            CompletedLast30Days = (int?)load.CompletedLast30Days ?? 0
            // ReSharper restore ConstantNullCoalescingCondition
            // ReSharper restore RedundantCast
        };

    private IQueryable<EmployeeRoleRow> AssignableEmployeeRoles() =>
        from employee in context.Set<Employee>().AsNoTracking()
        where employee.Roles.Any(role => role.CanFulfillWorkOrder)
        from role in employee.Roles
        select new EmployeeRoleRow
        {
            Id = employee.Id,
            UserName = employee.UserName,
            FirstName = employee.FirstName,
            MiddleName = employee.MiddleName,
            LastName = employee.LastName,
            EmailAddress = employee.EmailAddress,
            PreferredLanguage = employee.PreferredLanguage,
            RoleId = role.Id,
            RoleName = role.Name,
            RoleCanCreate = role.CanCreateWorkOrder,
            RoleCanFulfill = role.CanFulfillWorkOrder
        };

    private IQueryable<AssigneeLoad> AssigneeCounts()
    {
        var today = ChurchTimeZone.Today(timeProvider);
        return context.Set<WorkOrder>().AsNoTracking()
            .Where(workOrder => workOrder.Assignee != null)
            .Select(WorkOrderQueryFilters.AssigneeLoadFlags(today, timeProvider, CompletedWindowDays))
            .GroupBy(flag => flag.AssigneeId)
            .Select(group => new AssigneeLoad
            {
                AssigneeId = group.Key,
                AssignedAndOpen = group.Sum(flag => flag.Open),
                InProgress = group.Sum(flag => flag.InProgress),
                Overdue = group.Sum(flag => flag.Overdue),
                CompletedLast30Days = group.Sum(flag => flag.Completed)
            });
    }

    // ReSharper disable PropertyCanBeMadeInitOnly.Global -- materialized by EF Core from a MemberInit projection
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global -- setter required: EF Core materializes the projection
    private sealed class AssigneeLoad
    {
        public Guid AssigneeId { get; init; }
        public int AssignedAndOpen { get; init; }
        public int InProgress { get; init; }
        public int Overdue { get; init; }
        public int CompletedLast30Days { get; init; }
    }
    // ReSharper restore AutoPropertyCanBeMadeGetOnly.Global
    // ReSharper restore PropertyCanBeMadeInitOnly.Global
}
