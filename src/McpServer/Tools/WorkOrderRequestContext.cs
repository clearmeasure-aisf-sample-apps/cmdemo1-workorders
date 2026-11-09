using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.McpServer.Tools;

/// <summary>
/// The work order an MCP tool acts on and the employee executing the request, loaded by number and username.
/// </summary>
internal sealed record WorkOrderRequestContext(WorkOrder WorkOrder, Employee ExecutingUser)
{
    public static async Task<(WorkOrderRequestContext? Context, string? Error)> LoadAsync(
        IBus bus,
        string workOrderNumber,
        string executingUsername)
    {
        var workOrder = await bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
        if (workOrder == null)
        {
            return (null, $"No work order found with number '{workOrderNumber}'.");
        }

        var executingUser = await EmployeeLookup.FindAsync(bus, executingUsername);
        return executingUser == null
            ? (null, $"Employee with username '{executingUsername}' not found.")
            : (new WorkOrderRequestContext(workOrder, executingUser), null);
    }

    /// <summary>As <see cref="LoadAsync"/>, for the edit tools, which require a work-order number.</summary>
    public static Task<(WorkOrderRequestContext? Context, string? Error)> LoadForEditAsync(
        IBus bus,
        string workOrderNumber,
        string executingUsername) =>
        string.IsNullOrWhiteSpace(workOrderNumber)
            ? Task.FromResult<(WorkOrderRequestContext?, string?)>((null, "Work order number is required."))
            : LoadAsync(bus, workOrderNumber, executingUsername);
}
