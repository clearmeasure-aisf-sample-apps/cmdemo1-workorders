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
    public required string PeerDraft { get; init; }
    public required string PeerAssigneeInProgress { get; init; }

    /// <summary>Every number this scenario seeded (owned and peer).</summary>
    public IReadOnlyList<string> AllSeeded =>
    [
        CreatorDraft,
        AssigneeAssigned,
        AssigneeInProgress,
        CreatorComplete,
        PeerDraft,
        PeerAssigneeInProgress
    ];

    /// <summary>Orders whose creator is the scenario creator.</summary>
    public IReadOnlyList<string> CreatorOwned =>
        [CreatorDraft, AssigneeAssigned, AssigneeInProgress, CreatorComplete];

    /// <summary>Orders whose assignee is the scenario assignee (includes the peer In Progress).</summary>
    public IReadOnlyList<string> AssigneeOwned =>
        [AssigneeAssigned, AssigneeInProgress, PeerAssigneeInProgress];
}
