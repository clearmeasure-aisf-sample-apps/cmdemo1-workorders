using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;

namespace ClearMeasure.Bootcamp.McpServer.Tools.Arguments;

/// <summary>
/// Why an MCP tool may not edit a work order: only the creator of a draft may save or update it.
/// </summary>
internal static class DraftEditDenial
{
    /// <summary>The message when <see cref="SaveDraftCommand"/> is not valid for a save-work-order request.</summary>
    public static string? ForSave(WorkOrder workOrder, Employee executingUser) =>
        new SaveDraftCommand(workOrder, executingUser).IsValid() ? null : InvalidStatus(workOrder);

    /// <summary>
    /// The message when <see cref="SaveDraftCommand"/> is not valid for an update-work-order request: a draft updated
    /// by someone other than its creator gets a permission message; any other status the invalid-status message.
    /// </summary>
    public static string? ForUpdate(WorkOrder workOrder, Employee executingUser) =>
        new SaveDraftCommand(workOrder, executingUser).IsValid() ? null : UpdateNotAllowed(workOrder);

    public static string UpdateNotAllowed(WorkOrder workOrder)
    {
        if (workOrder.Status != WorkOrderStatus.Draft)
        {
            return InvalidStatus(workOrder);
        }

        var creatorUserName = workOrder.Creator?.UserName;
        return string.IsNullOrWhiteSpace(creatorUserName)
            ? $"Only the creator can update draft work order '{workOrder.Number}'."
            : $"Only the creator '{creatorUserName}' can update draft work order '{workOrder.Number}'.";
    }

    private static string InvalidStatus(WorkOrder workOrder) =>
        $"Command '{SaveDraftCommand.Name}' cannot be executed. Work order is in '{workOrder.Status.FriendlyName}' " +
        $"status but the command requires '{WorkOrderStatus.Draft.FriendlyName}' status.";
}
