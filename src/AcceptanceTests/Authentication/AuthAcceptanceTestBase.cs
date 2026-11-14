using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

/// <summary>
/// Authentication acceptance base: API-backed employees keyed by <see cref="AcceptanceTestBase.TestTag"/>,
/// with SQL fallback when test-support returns 404 (tdd/uat/prod).
/// </summary>
public abstract class AuthAcceptanceTestBase : AcceptanceApiTestBase
{
    /// <summary>
    /// Last name that sorts before A–Z so the seeded employee stays in the first EmployeeGetAll page.
    /// </summary>
    protected string DirectoryFirstLastName => $"!{TestTag}";

    /// <summary>Creates an employee that appears in the login directory page.</summary>
    protected async Task<TestSupportEmployee> SeedDirectoryEmployeeAsync(
        string? firstName = null,
        string? lastName = null,
        string? middleName = null)
    {
        var employee = await AcceptanceEmployeeSeed.CreateAsync(
            TestTag,
            new TestSupportEmployeeFields
            {
                FirstName = firstName ?? "Pat",
                LastName = lastName ?? DirectoryFirstLastName,
                MiddleName = middleName,
                AdditionalCreateRoleNames = []
            });
        return AcceptanceEmployeeSeed.ToDirectoryDto(employee);
    }
}
