using System.ComponentModel;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ModelContextProtocol.Server;

namespace ClearMeasure.Bootcamp.McpServer.Tools;

[McpServerToolType]
public class EmployeeTools
{
    [McpServerTool(Name = "list-employees"), Description("Lists all employees in the system with their username, name, email, and roles.")]
    public static async Task<string> ListEmployees(IBus bus)
    {
        var employees = await bus.Send(new EmployeeGetAllQuery());
        return JsonSerializer.Serialize(employees.Select(EmployeeJson.From).ToArray(), McpJsonContext.Default.EmployeeJsonArray);
    }

    [McpServerTool(Name = "get-employee"), Description("Retrieves a single employee by username.")]
    public static async Task<string> GetEmployee(
        IBus bus,
        [Description("The employee's username")] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return $"No employee found with username '{username}'.";
        }

        try
        {
            var employee = await bus.Send(new EmployeeByUserNameQuery(username));
            return JsonSerializer.Serialize(EmployeeJson.From(employee), McpJsonContext.Default.EmployeeJson);
        }
        catch (InvalidOperationException)
        {
            return $"No employee found with username '{username}'.";
        }
    }
}
