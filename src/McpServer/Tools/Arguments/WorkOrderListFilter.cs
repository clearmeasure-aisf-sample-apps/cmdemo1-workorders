using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.McpServer.Tools.Arguments;

/// <summary>
/// The optional filters of list-work-orders: a status key and the usernames of the creator and the assignee. Blank
/// filters are ignored.
/// </summary>
internal sealed record WorkOrderListFilter(string? Status, string? CreatorUsername, string? AssigneeUsername)
{
    /// <summary>The matching query, or <c>null</c> when a named creator or assignee does not exist (nothing matches).</summary>
    public async Task<WorkOrderSpecificationQuery?> ToQueryAsync(IBus bus)
    {
        var query = new WorkOrderSpecificationQuery();
        if (!string.IsNullOrWhiteSpace(Status))
        {
            query.MatchStatus(WorkOrderStatus.FromKey(Status));
        }

        var matched = await MatchEmployeeAsync(bus, CreatorUsername, query.MatchCreator)
                      && await MatchEmployeeAsync(bus, AssigneeUsername, query.MatchAssignee);
        return matched ? query : null;
    }

    private static async Task<bool> MatchEmployeeAsync(IBus bus, string? username, Action<Employee> match)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return true;
        }

        var employee = await EmployeeLookup.FindAsync(bus, username);
        if (employee == null)
        {
            return false;
        }

        match(employee);
        return true;
    }
}
