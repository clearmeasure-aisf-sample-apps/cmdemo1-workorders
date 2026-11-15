using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.McpServer.Tools.Arguments;

/// <summary>
/// A building name given to an MCP tool: trimmed. Blank means no building. Length is checked by
/// <see cref="WorkOrderFieldRules"/> (and by SaveDraft on the bus).
/// </summary>
internal readonly record struct BuildingArgument(string? Value, string? Error)
{
    public static BuildingArgument Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new BuildingArgument(null, null);
        }

        var trimmed = text.Trim();
        return WorkOrderFieldRules.BuildingLengthError(trimmed) is { } error
            ? new BuildingArgument(null, error)
            : new BuildingArgument(trimmed, null);
    }
}
