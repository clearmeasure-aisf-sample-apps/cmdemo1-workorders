using ClearMeasure.Bootcamp.DataAccess.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.TestSupport;

[TestFixture]
public class TestSupportWorkOrderDtoTests
{
    [Test]
    public void ShouldRoundTrip_WhenInitialized()
    {
        var dto = new TestSupportWorkOrderDto
        {
            Id = Guid.NewGuid(),
            Number = "WO1",
            Title = "t",
            Description = "d",
            RoomNumber = "2",
            CreatorUserName = "c"
        };
        dto.Number.ShouldBe("WO1");
        dto.CreatorUserName.ShouldBe("c");
    }
}
