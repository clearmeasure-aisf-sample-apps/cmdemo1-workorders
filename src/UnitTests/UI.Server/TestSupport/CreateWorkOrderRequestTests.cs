using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class CreateWorkOrderRequestTests
{
    [Test]
    public void ShouldHoldOptionalFields_WhenSet()
    {
        var request = new CreateWorkOrderRequest
        {
            TestTag = "t",
            CreatorUserName = "c",
            Title = "ti",
            Building = "Hall",
            StatusKey = "Assigned",
            AssigneeUserName = "u"
        };
        request.Title.ShouldBe("ti");
        request.CreatorUserName.ShouldBe("c");
        request.TestTag.ShouldBe("t");
        request.Building.ShouldBe("Hall");
        request.StatusKey.ShouldBe("Assigned");
        request.AssigneeUserName.ShouldBe("u");
    }
}
