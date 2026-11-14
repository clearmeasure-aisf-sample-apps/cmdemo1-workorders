namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

/// <summary>Optional fields when creating a draft work order through MCP in acceptance tests.</summary>
internal sealed class McpDraftCreateSpec
{
    public string? Instructions { get; init; }
    public string? RoomNumber { get; init; }
    public string? DueDate { get; init; }
}
