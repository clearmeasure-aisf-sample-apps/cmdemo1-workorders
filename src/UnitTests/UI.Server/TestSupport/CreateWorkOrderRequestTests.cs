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
            Description = "desc",
            RoomNumber = "12",
            Instructions = "fix",
            Number = "WO-1",
            Building = "Hall",
            StatusKey = "Assigned",
            Priority = "High",
            DueDate = "2026-11-13",
            AssigneeUserName = "u",
            CreatedDate = "2026-11-01T00:00:00Z",
            CompletedDate = "2026-11-02T00:00:00Z"
        };
        request.Title.ShouldBe("ti");
        request.Description.ShouldBe("desc");
        request.RoomNumber.ShouldBe("12");
        request.Instructions.ShouldBe("fix");
        request.Number.ShouldBe("WO-1");
        request.CreatorUserName.ShouldBe("c");
        request.TestTag.ShouldBe("t");
        request.Building.ShouldBe("Hall");
        request.StatusKey.ShouldBe("Assigned");
        request.Priority.ShouldBe("High");
        request.DueDate.ShouldBe("2026-11-13");
        request.AssigneeUserName.ShouldBe("u");
        request.CreatedDate.ShouldBe("2026-11-01T00:00:00Z");
        request.CompletedDate.ShouldBe("2026-11-02T00:00:00Z");
    }
}
