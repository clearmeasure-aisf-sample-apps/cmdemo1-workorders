namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>Body for <c>POST /api/test-support/employees</c>.</summary>
public sealed class CreateEmployeeRequest
{
    /// <summary>Unique tag for the calling acceptance test.</summary>
    public string TestTag { get; init; } = string.Empty;

    /// <summary>Whether the admin role may create work orders.</summary>
    public bool CanCreateWorkOrder { get; init; } = true;

    /// <summary>Whether the admin role may fulfill work orders.</summary>
    public bool CanFulfillWorkOrder { get; init; } = true;

    /// <summary>Optional given name.</summary>
    public string? FirstName { get; init; }

    /// <summary>Optional family name.</summary>
    public string? LastName { get; init; }

    /// <summary>Optional middle name.</summary>
    public string? MiddleName { get; init; }

    /// <summary>Optional extra create-only role names.</summary>
    public string[]? AdditionalCreateRoleNames { get; init; }
}
