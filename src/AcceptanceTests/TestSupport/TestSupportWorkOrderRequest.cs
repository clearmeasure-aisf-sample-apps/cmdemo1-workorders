namespace ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

/// <summary>Body for creating a work order through test-support.</summary>
public sealed class TestSupportWorkOrderRequest
{
    /// <summary>Unique tag for the calling acceptance test.</summary>
    public required string TestTag { get; init; }

    /// <summary>Creator user name.</summary>
    public required string CreatorUserName { get; init; }

    /// <summary>Optional title.</summary>
    public string? Title { get; init; }

    /// <summary>Optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Optional room number.</summary>
    public string? RoomNumber { get; init; }

    /// <summary>Optional instructions.</summary>
    public string? Instructions { get; init; }

    /// <summary>Optional explicit work-order number.</summary>
    public string? Number { get; init; }

    /// <summary>Optional status key; defaults to Draft.</summary>
    public string? StatusKey { get; init; }

    /// <summary>Optional priority name; defaults to Medium.</summary>
    public string? Priority { get; init; }

    /// <summary>Optional due date (ISO <c>yyyy-MM-dd</c>).</summary>
    public string? DueDate { get; init; }

    /// <summary>Optional assignee user name.</summary>
    public string? AssigneeUserName { get; init; }

    /// <summary>Anonymous payload that reads every property for JSON posts.</summary>
    public object ToJsonBody() => new
    {
        testTag = TestTag,
        creatorUserName = CreatorUserName,
        title = Title,
        description = Description,
        roomNumber = RoomNumber,
        instructions = Instructions,
        number = Number,
        statusKey = StatusKey,
        priority = Priority,
        dueDate = DueDate,
        assigneeUserName = AssigneeUserName
    };
}
