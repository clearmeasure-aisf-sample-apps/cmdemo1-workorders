namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

/// <summary>Expected seeded numbers and optional list-work-orders filters for portal/MCP parity.</summary>
internal sealed class McpListFilterExpectation
{
    public required IReadOnlyCollection<string> ExpectedNumbers { get; init; }
    public string? CreatorUsername { get; init; }
    public string? AssigneeUsername { get; init; }
    public string? Status { get; init; }
}
