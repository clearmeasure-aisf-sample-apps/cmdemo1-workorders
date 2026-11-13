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
        request.AdditionalCreateRoleNames.ShouldBeNull();
    }

    [Test]
    public void ShouldHoldAdditionalCreateRoleNames_WhenSet()
    {
        var roles = new[] { "Pastor", "Requester" };
        var request = new CreateEmployeeRequest(TestTag: "x", AdditionalCreateRoleNames: roles);
        request.AdditionalCreateRoleNames.ShouldBe(roles);
    }
}
