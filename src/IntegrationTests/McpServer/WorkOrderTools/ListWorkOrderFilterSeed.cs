namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer.WorkOrderTools;

/// <summary>Lookup of unique numbers for the list-filter catalog seeded by <see cref="McpWorkOrderSteps"/>.</summary>
public sealed class ListWorkOrderFilterSeed(
    string creatorUserName,
    string assigneeUserName,
    IReadOnlyDictionary<string, string> numbers)
{
    /// <summary>Primary creator user name.</summary>
    public string CreatorUserName { get; } = creatorUserName;

    /// <summary>Primary assignee user name.</summary>
    public string AssigneeUserName { get; } = assigneeUserName;

    /// <summary>Number for a filter-role key such as <c>LJWIP</c>.</summary>
    public string this[string key] => numbers[key];

    /// <summary>Every seeded work-order number.</summary>
    public string[] AllNumbers => numbers.Values.ToArray();
}
