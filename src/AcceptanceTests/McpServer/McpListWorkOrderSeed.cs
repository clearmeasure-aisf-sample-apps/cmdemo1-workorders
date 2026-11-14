namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

/// <summary>TestTag-scoped work-order numbers for MCP list vs portal filter coverage.</summary>
internal sealed class McpListWorkOrderSeed
{
    public required string CreatorUserName { get; init; }
    public required string AssigneeUserName { get; init; }
    public required string CreatorFullName { get; init; }
    public required string CreatorDraft { get; init; }
    public required string AssigneeAssigned { get; init; }
    public required string AssigneeInProgress { get; init; }
    public required string CreatorComplete { get; init; }
}
