namespace ClearMeasure.Bootcamp.DataAccess.TestSupport;

/// <summary>Employee created for an acceptance test.</summary>
public sealed class TestSupportEmployeeDto
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
