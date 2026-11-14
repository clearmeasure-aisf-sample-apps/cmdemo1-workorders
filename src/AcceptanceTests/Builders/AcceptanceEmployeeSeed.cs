using System.Net;
using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;
using ClearMeasure.Bootcamp.AcceptanceTests.TestUsers;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Builders;

/// <summary>
/// Creates acceptance employees via test-support when available, otherwise SQL
/// (<see cref="AcceptanceTestEmployees.Add"/>) for tdd/uat/prod where the API returns 404.
/// </summary>
public static class AcceptanceEmployeeSeed
{
    /// <summary>
    /// Probes test-support, then creates an employee through the API or SQL fallback.
    /// </summary>
    public static async Task<Employee> CreateAsync(string testTag, TestSupportEmployeeFields? fields = null)
    {
        var client = AcceptanceTestSupportSettings.CreateClient();
        if (await client.ProbeStatusAsync() == HttpStatusCode.NotFound)
        {
            return AcceptanceTestEmployees.Add(testTag, fields);
        }

        var created = await client.CreateEmployeeAsync(testTag, fields);
        var employee = new Employee(created.UserName, created.FirstName, created.LastName, created.EmailAddress)
        {
            Id = created.Id
        };
        employee.AddRole(new Role("admin", true, true));
        return employee;
    }

    /// <summary>Maps a domain employee to the test-support DTO shape used by login fixtures.</summary>
    public static TestSupportEmployee ToDirectoryDto(Employee employee) => new()
    {
        Id = employee.Id,
        UserName = employee.UserName,
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        EmailAddress = employee.EmailAddress
    };
}
