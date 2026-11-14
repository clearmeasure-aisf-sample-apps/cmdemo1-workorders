namespace ClearMeasure.Bootcamp.DataAccess.TestSupport;

/// <summary>Inputs for creating a test work order.</summary>
public sealed class TestSupportWorkOrderCreate
{
    /// <summary>Unique tag for the calling acceptance test.</summary>
    public required string TestTag { get; init; }

    /// <summary>User name of the creator employee.</summary>
    public required string CreatorUserName { get; init; }

    /// <summary>Optional title; defaults to a tag-prefixed value.</summary>
    public string? Title { get; init; }

    /// <summary>Optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Optional room number.</summary>
    public string? RoomNumber { get; init; }

    /// <summary>Optional instructions.</summary>
    public string? Instructions { get; init; }

    /// <summary>Optional explicit work-order number; otherwise generated.</summary>
    public string? Number { get; init; }

    /// <summary>Optional status key (<c>Draft</c>, <c>Assigned</c>, …); defaults to Draft.</summary>
    public string? StatusKey { get; init; }

    /// <summary>Optional priority name (<c>Low</c>, <c>Medium</c>, <c>High</c>); defaults to Medium.</summary>
    public string? Priority { get; init; }

    /// <summary>Optional due date (ISO <c>yyyy-MM-dd</c>), including past dates for overdue seeds.</summary>
    public string? DueDate { get; init; }

    /// <summary>Optional assignee user name (required for Assigned/InProgress seeds).</summary>
    public string? AssigneeUserName { get; init; }
}
