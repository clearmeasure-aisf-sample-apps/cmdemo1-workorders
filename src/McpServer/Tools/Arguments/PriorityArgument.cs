using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.McpServer.Tools.Arguments;

/// <summary>
/// A work-order priority given to an MCP tool as text: Low, Medium or High, case-insensitive. Blank means Medium.
/// </summary>
internal readonly record struct PriorityArgument(WorkOrderPriority Value, string? Error)
{
    public static PriorityArgument Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new PriorityArgument(WorkOrderPriority.Medium, null);
        }

        var trimmed = text.Trim();
        var match = Enum.GetNames<WorkOrderPriority>()
            .FirstOrDefault(name => string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase));
        return match != null
            ? new PriorityArgument(Enum.Parse<WorkOrderPriority>(match), null)
            : new PriorityArgument(
                WorkOrderPriority.Medium,
                $"Invalid priority '{text}'. Valid values are: Low, Medium, High.");
    }
}
