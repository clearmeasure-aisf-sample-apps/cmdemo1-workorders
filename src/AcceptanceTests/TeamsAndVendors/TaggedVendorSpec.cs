namespace ClearMeasure.Bootcamp.AcceptanceTests.TeamsAndVendors;

/// <summary>Fields for a TestTag-scoped vendor create in Teams &amp; Vendors fixtures.</summary>
public sealed class TaggedVendorSpec
{
    /// <summary>Name suffix before the test tag.</summary>
    public required string NameSuffix { get; init; }

    /// <summary>Optional trade.</summary>
    public string? Trade { get; init; }

    /// <summary>Optional contact name.</summary>
    public string? ContactName { get; init; }

    /// <summary>Optional email.</summary>
    public string? EmailAddress { get; init; }

    /// <summary>Optional phone.</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Whether the vendor is active; defaults to true.</summary>
    public bool IsActive { get; init; } = true;
}
