namespace ClearMeasure.Bootcamp.McpServer.Tools.Arguments;

/// <summary>
/// The arguments of execute-work-order-command: which state command to run on which work order, by whom, and the
/// assignee or reason some commands require.
/// </summary>
internal sealed record WorkOrderCommandRequest(
    string WorkOrderNumber,
    string CommandName,
    string ExecutingUsername,
    string? AssigneeUsername,
    string? Reason)
{
    public const string DraftToAssigned = "DraftToAssignedCommand";

    public bool AssignsTheWorkOrder => CommandName == DraftToAssigned;

    public ShelveReason ShelveReason => ShelveReason.For(CommandName, Reason);
}
