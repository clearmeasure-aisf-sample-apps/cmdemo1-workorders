namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>Body for <c>POST /api/test-support/work-orders</c>.</summary>
public sealed class CreateWorkOrderRequest
{
    /// <summary>Unique tag for the calling acceptance test.</summary>
    public string TestTag { get; set; } = string.Empty;

    /// <summary>User name of the creator employee.</summary>
    public string CreatorUserName { get; set; } = string.Empty;

    /// <summary>Optional title.</summary>
    public string? Title { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional room number.</summary>
    public string? RoomNumber { get; set; }
}
