using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

/// <summary>
/// Authentication acceptance base: API-backed employees keyed by <see cref="AcceptanceTestBase.TestTag"/>.
/// </summary>
public abstract class AuthAcceptanceTestBase : AcceptanceApiTestBase
{
    /// <summary>
    /// Last name that sorts before A–Z so the seeded employee stays in the first EmployeeGetAll page.
    /// </summary>
    protected string DirectoryFirstLastName => $"!{TestTag}";

    /// <summary>Creates an employee that appears in the login directory page.</summary>
    protected Task<TestSupportEmployee> SeedDirectoryEmployeeAsync(
        string? firstName = null,
        string? lastName = null,
        string? middleName = null) =>
        Mother.EmployeeAsync(new TestSupportEmployeeFields
        {
            FirstName = firstName ?? "Pat",
            LastName = lastName ?? DirectoryFirstLastName,
            MiddleName = middleName
        });
}
