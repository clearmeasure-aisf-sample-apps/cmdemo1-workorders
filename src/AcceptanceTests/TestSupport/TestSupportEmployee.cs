namespace ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

/// <summary>Employee DTO from test-support.</summary>
public sealed class TestSupportEmployee
{
    /// <summary>Persisted id.</summary>
    public Guid Id { get; init; }

    /// <summary>Login user name.</summary>
    public required string UserName { get; init; }

    /// <summary>Given name.</summary>
    public required string FirstName { get; init; }

    /// <summary>Family name.</summary>
    public required string LastName { get; init; }

    /// <summary>Email.</summary>
    public required string EmailAddress { get; init; }
}
