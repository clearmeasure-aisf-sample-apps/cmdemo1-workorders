using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.McpServer.Tools;

/// <summary>
/// Finds the employee an MCP tool names by username. An unknown username is not an error to the tools: they answer
/// with a "not found" message, so a missing employee is <c>null</c>.
/// </summary>
internal static class EmployeeLookup
{
    public static async Task<Employee?> FindAsync(IBus bus, string username)
    {
        try
        {
            return await bus.Send(new EmployeeByUserNameQuery(username));
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
