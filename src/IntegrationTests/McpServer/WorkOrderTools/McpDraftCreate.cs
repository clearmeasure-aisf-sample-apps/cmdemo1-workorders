namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer.WorkOrderTools;

/// <summary>Arguments for creating a draft through the MCP create-work-order tool.</summary>
public sealed class McpDraftCreate
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? RoomNumber { get; init; }
    public string? DueDate { get; init; }
    public string? Instructions { get; init; }
}
