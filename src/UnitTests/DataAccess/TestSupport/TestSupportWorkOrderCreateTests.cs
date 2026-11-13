using ClearMeasure.Bootcamp.DataAccess.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.TestSupport;

[TestFixture]
public class TestSupportWorkOrderCreateTests
{
    [Test]
    public void ShouldRoundTrip_WhenInitialized()
    {
        var request = new TestSupportWorkOrderCreate
        {
            TestTag = "t",
            CreatorUserName = "c",
            Title = "title",
            Description = "d",
            RoomNumber = "1"
        };
        request.CreatorUserName.ShouldBe("c");
        request.Title.ShouldBe("title");
    }
}
