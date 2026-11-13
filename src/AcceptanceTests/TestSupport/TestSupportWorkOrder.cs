namespace ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

/// <summary>Work-order DTO from test-support.</summary>
public sealed class TestSupportWorkOrder
{
    /// <summary>Persisted id.</summary>
    public Guid Id { get; init; }

    /// <summary>Work order number.</summary>
    public required string Number { get; init; }

    /// <summary>Title.</summary>
    public required string Title { get; init; }

    /// <summary>Description.</summary>
    public string? Description { get; init; }

    /// <summary>Room number.</summary>
    public string? RoomNumber { get; init; }

    /// <summary>Creator user name.</summary>
    public required string CreatorUserName { get; init; }
}
