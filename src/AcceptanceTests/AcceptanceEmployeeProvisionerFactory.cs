using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;
using ClearMeasure.Bootcamp.AcceptanceTests.TestUsers;
using ClearMeasure.Bootcamp.UnitTests.Acceptance;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Builds the default <see cref="AcceptanceEmployeeProvisioner"/> aimed at the acceptance host.
/// </summary>
public static class AcceptanceEmployeeProvisionerFactory
{
    /// <summary>Default provisioner: probe the live host, then test-support or SQL.</summary>
    public static AcceptanceEmployeeProvisioner CreateDefault()
    {
        var client = AcceptanceTestSupportSettings.CreateClient();
        return new AcceptanceEmployeeProvisioner(
            client.ProbeStatusAsync,
            testTag => CreateViaTestSupportAsync(client, testTag),
            AcceptanceTestEmployees.Add);
    }

    private static async Task<Employee> CreateViaTestSupportAsync(TestSupportClient client, string testTag)
    {
        var created = await new AcceptanceObjectMother(client, testTag).EmployeeAsync();
        var employee = new Employee(created.UserName, created.FirstName, created.LastName, created.EmailAddress)
        {
            Id = created.Id
        };
        employee.AddRole(new Role("admin", true, true));
        return employee;
    }
}
