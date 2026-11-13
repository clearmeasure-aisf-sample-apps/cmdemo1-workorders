// ReSharper disable UnusedAutoPropertyAccessor.Global -- object initializer and test-support JSON payload fields
namespace ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

/// <summary>Optional fields when creating an employee through test-support.</summary>
public sealed class TestSupportEmployeeFields
{
    /// <summary>Optional given name.</summary>
    public string? FirstName { get; init; }

    /// <summary>Optional family name.</summary>
    public string? LastName { get; init; }

    /// <summary>Optional middle name.</summary>
    public string? MiddleName { get; init; }

    /// <summary>Optional extra create-only role names.</summary>
    public string[]? AdditionalCreateRoleNames { get; init; }
}
