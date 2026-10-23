using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class UserDisplayTests
{
    [Test]
    public void Initials_UsesFirstAndLastName()
    {
        UserDisplay.Initials(new Employee("ned", "ned", "Flanders", "n@x")).ShouldBe("NF");
    }

    [Test]
    public void Initials_FallsBackToUserName_ThenQuestionMark()
    {
        UserDisplay.Initials(null, "hsimpson").ShouldBe("HS");
        UserDisplay.Initials(new Employee("x1", "", "", "e")).ShouldBe("X1");
        UserDisplay.Initials(null).ShouldBe("?");
    }

    [Test]
    public void Roles_JoinsSortedNames_OrEmpty()
    {
        var employee = new Employee("t", "T", "L", "e");
        employee.AddRole(new Role("Requester", true, false));
        employee.AddRole(new Role("Pastor", true, false));

        UserDisplay.Roles(employee).ShouldBe("Pastor · Requester");
        UserDisplay.Roles(new Employee("t", "T", "L", "e")).ShouldBe(string.Empty);
        UserDisplay.Roles(null).ShouldBe(string.Empty);
    }
}
