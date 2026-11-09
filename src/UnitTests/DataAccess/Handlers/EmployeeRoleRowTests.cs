using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.Handlers;

[TestFixture]
public class EmployeeRoleRowTests
{
    private static readonly Guid EmployeeId = Guid.NewGuid();

    [Test]
    public void ShouldBuildOneEmployeeWithEveryRole_WhenTheRowsBelongToOneEmployee()
    {
        EmployeeRoleRow[] rows = [Row("Fulfiller", canFulfill: true), Row("Viewer", canFulfill: false)];

        var employee = EmployeeRoleRow.ToEmployee(rows);

        employee.Id.ShouldBe(EmployeeId);
        employee.UserName.ShouldBe("gwillie");
        employee.MiddleName.ShouldBe("M");
        employee.PreferredLanguage.ShouldBe("es-MX");
        employee.Roles.Select(role => role.Name).ShouldBe(["Fulfiller", "Viewer"], ignoreOrder: true);
        employee.CanFulfillWorkOrder().ShouldBeTrue();
    }

    private static EmployeeRoleRow Row(string roleName, bool canFulfill) => new()
    {
        Id = EmployeeId,
        UserName = "gwillie",
        FirstName = "Groundskeeper",
        MiddleName = "M",
        LastName = "Willie",
        EmailAddress = "willie@example.com",
        PreferredLanguage = "es-MX",
        RoleId = Guid.NewGuid(),
        RoleName = roleName,
        RoleCanCreate = false,
        RoleCanFulfill = canFulfill
    };
}
