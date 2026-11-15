using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.McpServer.Tools.Arguments;

/// <summary>
/// Why an in-progress work order goes back to assigned. Shelve and InProgressToAssignedCommand require a reason;
/// length and required checks come from <see cref="WorkOrderFieldRules"/>.
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
        return WorkOrderFieldRules.McpShelveReasonError(trimmed) is { } error
            ? new ShelveReason(null, error)
            : new ShelveReason(trimmed, null);
    }
}
