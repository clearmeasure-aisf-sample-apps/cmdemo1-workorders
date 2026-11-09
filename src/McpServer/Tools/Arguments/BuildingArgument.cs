using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.McpServer.Tools.Arguments;

/// <summary>
/// A building name given to an MCP tool: trimmed, at most <see cref="WorkOrder.BuildingMaxLength"/> characters.
/// Blank means no building.
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
        return trimmed.Length > WorkOrder.BuildingMaxLength
            ? new BuildingArgument(null, $"Building cannot exceed {WorkOrder.BuildingMaxLength} characters.")
            : new BuildingArgument(trimmed, null);
    }
}
