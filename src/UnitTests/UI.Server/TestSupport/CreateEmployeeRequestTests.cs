using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class CreateEmployeeRequestTests
{
    [Test]
    public void ShouldDefaultPermissions_WhenConstructed()
    {
        var request = new CreateEmployeeRequest { TestTag = "x" };
        request.CanCreateWorkOrder.ShouldBeTrue();
        request.CanFulfillWorkOrder.ShouldBeTrue();
        request.FirstName.ShouldBeNull();
        request.LastName.ShouldBeNull();
        request.MiddleName.ShouldBeNull();
    }

    [Test]
    public void ShouldHoldNames_WhenProvided()
    {
        var request = new CreateEmployeeRequest
        {
            TestTag = "x",
            FirstName = "Ned",
            LastName = "!x",
            MiddleName = "F"
        };
        request.FirstName.ShouldBe("Ned");
        request.LastName.ShouldBe("!x");
        request.MiddleName.ShouldBe("F");
    }
}
