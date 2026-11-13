using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class CreateEmployeeRequestTests
{
    [Test]
    public void ShouldDefaultPermissions_WhenConstructed()
    {
        var request = new CreateEmployeeRequest(TestTag: "x");
        request.CanCreateWorkOrder.ShouldBeTrue();
        request.CanFulfillWorkOrder.ShouldBeTrue();
    }
}
