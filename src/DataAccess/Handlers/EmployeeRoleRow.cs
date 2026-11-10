using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// One role of one employee, projected flat by EF Core: an employee with three roles is three rows.
/// </summary>
internal sealed class EmployeeRoleRow
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = "";
    public string FirstName { get; init; } = "";
    public string? MiddleName { get; init; }
    public string LastName { get; init; } = "";
    public string EmailAddress { get; init; } = "";
    public string PreferredLanguage { get; init; } = "";
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = "";
    public bool RoleCanCreate { get; init; }
    public bool RoleCanFulfill { get; init; }

    /// <summary>The employee of a group of rows, with every role of the group.</summary>
    public static Employee ToEmployee(IReadOnlyCollection<EmployeeRoleRow> rowsOfOneEmployee)
    {
        var first = rowsOfOneEmployee.First();
        var employee = new Employee(first.UserName, first.FirstName, first.LastName, first.EmailAddress)
        {
            Id = first.Id,
            MiddleName = first.MiddleName,
            PreferredLanguage = first.PreferredLanguage
        };
        foreach (var row in rowsOfOneEmployee)
        {
            employee.AddRole(new Role(row.RoleName, row.RoleCanCreate, row.RoleCanFulfill) { Id = row.RoleId });
        }

        return employee;
    }
}
