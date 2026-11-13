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
}
