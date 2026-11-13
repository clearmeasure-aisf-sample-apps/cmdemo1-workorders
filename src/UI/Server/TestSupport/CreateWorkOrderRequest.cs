namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>Body for <c>POST /api/test-support/work-orders</c>.</summary>
public sealed class CreateWorkOrderRequest
{
    /// <summary>Unique tag for the calling acceptance test.</summary>
    public string TestTag { get; init; } = string.Empty;

    /// <summary>User name of the creator employee.</summary>
    public string CreatorUserName { get; init; } = string.Empty;

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

    /// <summary>Optional building name.</summary>
    public string? Building { get; init; }

    /// <summary>Optional created date (ISO-8601).</summary>
    public string? CreatedDate { get; init; }

    /// <summary>Optional completed date (ISO-8601).</summary>
    public string? CompletedDate { get; init; }
}
