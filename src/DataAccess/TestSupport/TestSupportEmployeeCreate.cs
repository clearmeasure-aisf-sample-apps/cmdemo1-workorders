namespace ClearMeasure.Bootcamp.DataAccess.TestSupport;

/// <summary>Inputs for creating a test employee.</summary>
public sealed class TestSupportEmployeeCreate
{
    /// <summary>Unique tag for the calling acceptance test.</summary>
    public required string TestTag { get; init; }

    /// <summary>Whether the admin role may create work orders.</summary>
    public bool CanCreateWorkOrder { get; init; } = true;

    /// <summary>Whether the admin role may fulfill work orders.</summary>
    public bool CanFulfillWorkOrder { get; init; } = true;

    /// <summary>
    /// Extra create-only role names (for multi-role dropdown/duplicate-row coverage).
    /// Each role gets <c>CanCreateWorkOrder=true</c> and <c>CanFulfillWorkOrder=false</c>.
    /// </summary>
    public string[]? AdditionalCreateRoleNames { get; init; }
}
