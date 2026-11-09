using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.McpServer.Tools.Arguments;

namespace ClearMeasure.Bootcamp.McpServer.Tools;

/// <summary>
/// Resolves work-order state commands for MCP tool execution.
/// </summary>
internal static class WorkOrderCommandExecutor
{
    private const string AvailableCommandsList =
        "DraftToAssignedCommand, AssignedToInProgressCommand, InProgressToAssignedCommand, Shelve, InProgressToCompleteCommand, AssignedToCancelledCommand";

    private static readonly IReadOnlyDictionary<string, Func<WorkOrder, Employee, StateCommandBase>> CommandFactories =
        new Dictionary<string, Func<WorkOrder, Employee, StateCommandBase>>(StringComparer.Ordinal)
        {
            [WorkOrderCommandRequest.DraftToAssigned] = (workOrder, user) => new DraftToAssignedCommand(workOrder, user),
            ["AssignedToInProgressCommand"] = (workOrder, user) => new AssignedToInProgressCommand(workOrder, user),
            ["InProgressToAssignedCommand"] = (workOrder, user) => new InProgressToAssignedCommand(workOrder, user),
            ["Shelve"] = (workOrder, user) => new InProgressToAssignedCommand(workOrder, user),
            ["InProgressToCompleteCommand"] = (workOrder, user) => new InProgressToCompleteCommand(workOrder, user),
            ["AssignedToCancelledCommand"] = (workOrder, user) => new AssignedToCancelledCommand(workOrder, user),
        };

    internal static async Task<string> ExecuteCommandAsync(IBus bus, WorkOrderCommandRequest request)
    {
        var reason = request.ShelveReason;
        if (reason.Error != null)
        {
            return reason.Error;
        }

        var (context, error) = await WorkOrderRequestContext.LoadAsync(
            bus,
            request.WorkOrderNumber,
            request.ExecutingUsername);
        error ??= await AssignWhenRequestedAsync(bus, request, context?.WorkOrder);
        return error ?? await RunAsync(bus, request.CommandName, context!, reason.Text);
    }

    private static async Task<string?> AssignWhenRequestedAsync(IBus bus, WorkOrderCommandRequest request, WorkOrder? workOrder)
    {
        if (!request.AssignsTheWorkOrder || workOrder == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(request.AssigneeUsername))
        {
            return "DraftToAssignedCommand requires an assigneeUsername parameter.";
        }

        var assignee = await EmployeeLookup.FindAsync(bus, request.AssigneeUsername);
        if (assignee == null)
        {
            return $"Assignee with username '{request.AssigneeUsername}' not found.";
        }

        workOrder.Assignee = assignee;
        return null;
    }

    private static async Task<string> RunAsync(
        IBus bus,
        string commandName,
        WorkOrderRequestContext context,
        string? reason)
    {
        if (!CommandFactories.TryGetValue(commandName, out var factory))
        {
            return $"Unknown command '{commandName}'. Available commands: {AvailableCommandsList}.";
        }

        var command = factory(context.WorkOrder, context.ExecutingUser);
        if (!command.IsValid())
        {
            return $"Command '{commandName}' cannot be executed. Work order is in '{context.WorkOrder.Status.FriendlyName}' " +
                   $"status but the command requires '{command.GetBeginStatus().FriendlyName}' status.";
        }

        if (command is InProgressToAssignedCommand shelve)
        {
            command = shelve with { Reason = reason };
        }

        var result = await bus.Send(command);
        return WorkOrderJson.Detail(result.WorkOrder);
    }
}
