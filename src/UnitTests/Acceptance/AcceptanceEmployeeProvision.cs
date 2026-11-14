using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UnitTests.Acceptance;

/// <summary>
/// Result of provisioning the current-user employee for one browser fixture.
/// </summary>
/// <param name="Employee">The employee the fixture will sign in as.</param>
/// <param name="TestSupportAvailable">
/// True when the host exposed test-support (pool slot); false on tdd/uat/prod (404 probe).
/// </param>
public sealed record AcceptanceEmployeeProvision(Employee Employee, bool TestSupportAvailable);
