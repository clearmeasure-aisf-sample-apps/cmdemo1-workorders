namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>Body for <c>POST /api/test-support/employees</c>.</summary>
// ReSharper disable UnusedAutoPropertyAccessor.Global -- JSON request body; binders set and handlers read
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global -- model binding requires public setters
// ReSharper disable PropertyCanBeMadeInitOnly.Global -- model binding requires public setters
public sealed class CreateEmployeeRequest
{
    /// <summary>Unique tag for the calling acceptance test.</summary>
    public string TestTag { get; set; } = string.Empty;

    /// <summary>Whether the admin role may create work orders.</summary>
    public bool CanCreateWorkOrder { get; set; } = true;

    /// <summary>Whether the admin role may fulfill work orders.</summary>
    public bool CanFulfillWorkOrder { get; set; } = true;
}
