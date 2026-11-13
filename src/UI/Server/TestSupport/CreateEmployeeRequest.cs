namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>Body for <c>POST /api/test-support/employees</c>.</summary>
public sealed record CreateEmployeeRequest(
    string TestTag,
    bool CanCreateWorkOrder = true,
    bool CanFulfillWorkOrder = true);
