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
            RoomNumber = "1",
            Instructions = "i",
            Number = "WO1",
            StatusKey = "InProgress",
            Priority = "Low",
            DueDate = "2000-01-01",
            AssigneeUserName = "a"
        };
        request.CreatorUserName.ShouldBe("c");
        request.Title.ShouldBe("title");
        request.Instructions.ShouldBe("i");
        request.Number.ShouldBe("WO1");
        request.StatusKey.ShouldBe("InProgress");
        request.Priority.ShouldBe("Low");
        request.DueDate.ShouldBe("2000-01-01");
        request.AssigneeUserName.ShouldBe("a");
    }
}
