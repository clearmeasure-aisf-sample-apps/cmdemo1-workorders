namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>Body for <c>POST /api/test-support/work-orders</c>.</summary>
public sealed record CreateWorkOrderRequest(
    string TestTag,
    string CreatorUserName,
    string? Title = null,
    string? Description = null,
    string? RoomNumber = null);
