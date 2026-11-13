using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class CreateWorkOrderRequestTests
{
    [Test]
    public void ShouldHoldOptionalFields_WhenSet()
    {
        var request = new CreateWorkOrderRequest { TestTag = "t", CreatorUserName = "c", Title = "ti" };
        request.Title.ShouldBe("ti");
        request.CreatorUserName.ShouldBe("c");
    }
}
