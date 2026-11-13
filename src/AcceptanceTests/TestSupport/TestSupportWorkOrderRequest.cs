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
}
