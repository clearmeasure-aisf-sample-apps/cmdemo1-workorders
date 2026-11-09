using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class EmployeeHasRoleTests
{
    [Test]
    public void HasRole_WhenRoleNameIsNull_ShouldReturnFalse()
    {
        var employee = ObjectMother.Faker<Employee>();
        var role = ObjectMother.Faker<Role>();
        role.Name = "Manager";
        employee.AddRole(role);

        employee.HasRole(null).ShouldBeFalse();
    }

    [Test]
    public void HasRole_WhenRoleNameIsEmpty_ShouldReturnFalse()
    {
        var employee = ObjectMother.Faker<Employee>();
        var role = ObjectMother.Faker<Role>();
        role.Name = "Manager";
        employee.AddRole(role);

        employee.HasRole(string.Empty).ShouldBeFalse();
    }

    [Test]
    public void HasRole_WhenNameDiffersOnlyByCase_ShouldReturnTrue()
    {
        var employee = ObjectMother.Faker<Employee>();
        var role = ObjectMother.Faker<Role>();
        role.Name = "Manager";
        employee.AddRole(role);

        employee.HasRole("manager").ShouldBeTrue();
    }

    [Test]
    public void HasRole_WhenNoRoleNameEquals_ShouldReturnFalse()
    {
        var employee = ObjectMother.Faker<Employee>();
        var role = ObjectMother.Faker<Role>();
        role.Name = "Worker";
        employee.AddRole(role);

        employee.HasRole("Manager").ShouldBeFalse();
    }

    [Test]
    public void HasRole_WhenEmployeeHasNoRoles_ShouldReturnFalse()
    {
        var employee = ObjectMother.Faker<Employee>();

        employee.Roles.ShouldBeEmpty();
        employee.HasRole("Manager").ShouldBeFalse();
    }

    [Test]
    public void HasRole_WhenNameMatches_ShouldReturnTrue()
    {
        var employee = ObjectMother.Faker<Employee>();
        var role = ObjectMother.Faker<Role>();
        role.Name = "Manager";
        employee.AddRole(role);

        employee.HasRole("Manager").ShouldBeTrue();
    }

    [Test]
    public void HasRole_WhenALaterRoleMatches_ShouldReturnTrue()
    {
        var employee = ObjectMother.Faker<Employee>();
        var worker = ObjectMother.Faker<Role>();
        worker.Name = "Worker";
        employee.AddRole(worker);
        var manager = ObjectMother.Faker<Role>();
        manager.Name = "Manager";
        employee.AddRole(manager);

        employee.HasRole("manager").ShouldBeTrue();
    }

    [Test]
    public void HasRole_WhenRoleNameIsWhitespace_ShouldMatchOnlyThatName()
    {
        var employee = ObjectMother.Faker<Employee>();
        var role = ObjectMother.Faker<Role>();
        role.Name = " ";
        employee.AddRole(role);

        employee.HasRole(" ").ShouldBeTrue();
        employee.HasRole("Manager").ShouldBeFalse();
    }

    [Test]
    public void HasRole_WhenStoredRoleNameIsNull_ShouldReturnFalse()
    {
        var employee = ObjectMother.Faker<Employee>();
        var role = ObjectMother.Faker<Role>();
        role.Name = null!;
        employee.AddRole(role);

        employee.HasRole("Manager").ShouldBeFalse();
    }
}
