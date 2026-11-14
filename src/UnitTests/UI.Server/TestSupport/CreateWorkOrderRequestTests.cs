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
            Description = "d",
            RoomNumber = "r",
            Instructions = "i",
            Number = "WO1",
            StatusKey = "Assigned",
            Priority = "High",
            DueDate = "2099-01-01",
            AssigneeUserName = "a"
        };
        request.Title.ShouldBe("ti");
        request.CreatorUserName.ShouldBe("c");
        request.TestTag.ShouldBe("t");
        request.Description.ShouldBe("d");
        request.RoomNumber.ShouldBe("r");
        request.Instructions.ShouldBe("i");
        request.Number.ShouldBe("WO1");
        request.StatusKey.ShouldBe("Assigned");
        request.Priority.ShouldBe("High");
        request.DueDate.ShouldBe("2099-01-01");
        request.AssigneeUserName.ShouldBe("a");
    }
}
