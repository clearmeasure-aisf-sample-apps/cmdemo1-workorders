using ClearMeasure.Bootcamp.DataAccess.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.TestSupport;

[TestFixture]
public class TestSupportEmployeeCreateTests
{
    [Test]
    public void ShouldRoundTrip_WhenInitialized()
    {
        var request = new TestSupportEmployeeCreate
        {
            TestTag = "tag",
            CanCreateWorkOrder = true,
            CanFulfillWorkOrder = false,
            FirstName = "Homer",
            LastName = "!tag",
            MiddleName = "J",
            AdditionalCreateRoleNames = ["Pastor"]
        };
        request.TestTag.ShouldBe("tag");
        request.CanCreateWorkOrder.ShouldBeTrue();
        request.CanFulfillWorkOrder.ShouldBeFalse();
        request.FirstName.ShouldBe("Homer");
        request.LastName.ShouldBe("!tag");
        request.MiddleName.ShouldBe("J");
        request.AdditionalCreateRoleNames.ShouldBe(["Pastor"]);
    }
}
