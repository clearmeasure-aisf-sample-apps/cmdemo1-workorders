using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.TestSupport;

[TestFixture]
public class EmployeeBuilderTests
{
    [Test]
    public void ShouldBuildEmployee_WithUserNameAndDefaultEmail()
    {
        var employee = EmployeeBuilder.Create("willie")
            .WithFirstName("Groundskeeper")
            .WithLastName("Willie")
            .Build();

        employee.UserName.ShouldBe("willie");
        employee.FirstName.ShouldBe("Groundskeeper");
        employee.LastName.ShouldBe("Willie");
        employee.EmailAddress.ShouldBe("willie@example.test");
        employee.Roles.ShouldBeEmpty();
    }

    [Test]
    public void ShouldAddRolesAndId_WhenConfigured()
    {
        var id = Guid.NewGuid();

        var employee = EmployeeBuilder.Create("pastor")
            .WithId(id)
            .WithMiddleName("J")
            .WithRole("Pastor", canCreate: true, canFulfill: false)
            .WithRole("Worker", canCreate: false, canFulfill: true)
            .Build();

        employee.Id.ShouldBe(id);
        employee.MiddleName.ShouldBe("J");
        employee.Roles.Count.ShouldBe(2);
        employee.CanCreateWorkOrder().ShouldBeTrue();
        employee.CanFulfillWorkOrder().ShouldBeTrue();
    }
}
