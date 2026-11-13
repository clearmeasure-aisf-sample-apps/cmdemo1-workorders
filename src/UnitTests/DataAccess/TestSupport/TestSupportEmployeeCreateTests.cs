using ClearMeasure.Bootcamp.DataAccess.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.TestSupport;

[TestFixture]
public class TestSupportEmployeeCreateTests
{
    [Test]
    public void ShouldRoundTrip_WhenInitialized()
    {
        var roles = new[] { "Pastor", "Requester" };
        var request = new TestSupportEmployeeCreate
        {
            TestTag = "tag",
            CanCreateWorkOrder = true,
            CanFulfillWorkOrder = false,
            AdditionalCreateRoleNames = roles
        };
        request.TestTag.ShouldBe("tag");
        request.CanCreateWorkOrder.ShouldBeTrue();
        request.CanFulfillWorkOrder.ShouldBeFalse();
        request.AdditionalCreateRoleNames.ShouldBe(roles);
    }
}
