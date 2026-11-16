using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer.WorkOrderTools;

/// <summary>Field values for persisting a draft work order in MCP tool tests.</summary>
public sealed class DraftArrange
{
    public required string Title { get; init; }
    public WorkOrderPriority Priority { get; init; } = WorkOrderPriority.Medium;
    public string? Building { get; init; }
    public string? Room { get; init; }
    public DateOnly? DueDate { get; init; }
}
