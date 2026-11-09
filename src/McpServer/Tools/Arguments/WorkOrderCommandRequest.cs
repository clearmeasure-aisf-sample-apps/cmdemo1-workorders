namespace ClearMeasure.Bootcamp.McpServer.Tools.Arguments;

/// <summary>
/// The arguments of execute-work-order-command: which state command to run on which work order, by whom, and the
/// assignee or reason some commands require.
/// </summary>
internal sealed record WorkOrderCommandRequest
{
    public const string DraftToAssigned = "DraftToAssignedCommand";

    public required string WorkOrderNumber { get; init; }

    public required string CommandName { get; init; }

    public required string ExecutingUsername { get; init; }

    public string? AssigneeUsername { get; init; }

    public string? Reason { get; init; }

    public bool AssignsTheWorkOrder => CommandName == DraftToAssigned;

    public ShelveReason ShelveReason => ShelveReason.For(CommandName, Reason);
}
