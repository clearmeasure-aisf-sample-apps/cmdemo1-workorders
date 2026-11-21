using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class UserDisplayRolesTests
{
    [Test]
    public void Roles_RemovesCaseInsensitiveDuplicates_AndKeepsFirstSpelling()
    {
        var employee = EmployeeBuilder.Create("dup")
            .WithRole("Creator", true, false)
            .WithRole("creator", true, false)
            .WithRole("Fulfiller", false, true)
            .Build();

        UserDisplay.Roles(employee).ShouldBe("Creator · Fulfiller");
    }

    [Test]
    public void Roles_SkipsBlankNames()
    {
        var employee = EmployeeBuilder.Create("blank")
            .WithRole("Creator", true, false)
            .WithRole("", true, false)
            .WithRole("  ", false, true)
            .Build();

        UserDisplay.Roles(employee).ShouldBe("Creator");
    }

    [Test]
    public void Roles_ReturnsEmpty_WhenNoRoles()
    {
        var employee = EmployeeBuilder.Create("nobody").Build();

        UserDisplay.Roles(employee).ShouldBe(string.Empty);
    }
}
