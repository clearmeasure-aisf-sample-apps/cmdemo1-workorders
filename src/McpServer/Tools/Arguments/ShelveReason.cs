using ClearMeasure.Bootcamp.Core.Model.StateCommands;

namespace ClearMeasure.Bootcamp.McpServer.Tools.Arguments;

/// <summary>
/// Why an in-progress work order goes back to assigned. Shelve and InProgressToAssignedCommand require a reason of at
/// most <see cref="InProgressToAssignedCommand.ReasonMaxLength"/> characters (trimmed); other commands take none.
/// </summary>
internal readonly record struct ShelveReason(string? Text, string? Error)
{
    public static ShelveReason For(string commandName, string? reason)
    {
        if (commandName is not ("Shelve" or "InProgressToAssignedCommand"))
        {
            return new ShelveReason(null, null);
        }

        var trimmed = reason?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            return new ShelveReason(null, "Shelve requires a reason.");
        }

        return trimmed.Length > InProgressToAssignedCommand.ReasonMaxLength
            ? new ShelveReason(null, $"Reason cannot exceed {InProgressToAssignedCommand.ReasonMaxLength} characters.")
            : new ShelveReason(trimmed, null);
    }
}
