using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;

[TestFixture]
public class WorkOrderSearchTextTests
{
    [Test]
    public void ShouldMatchNumberTitleOrRoom_WhenQueryAppearsThere()
    {
        var match = WorkOrderSearchText.Matches("hall").Compile();

        match(new WorkOrder { Number = "HALL-1" }).ShouldBeTrue();
        match(new WorkOrder { Title = "Fix hall light" }).ShouldBeTrue();
        match(new WorkOrder { RoomNumber = "Hall-A" }).ShouldBeTrue();
        match(new WorkOrder { Title = "Kitchen" }).ShouldBeFalse();
    }

    [Test]
    public void ShouldMatchCreatorOrAssigneeName_WhenQueryAppearsInEither()
    {
        var creator = new Employee("c", "Ada", "Lovelace", "a@test.com");
        var assignee = new Employee("a", "Grace", "Hopper", "g@test.com");
        var match = WorkOrderSearchText.Matches("hop").Compile();

        match(new WorkOrder { Creator = creator, Assignee = assignee }).ShouldBeTrue();
        match(new WorkOrder { Creator = creator }).ShouldBeFalse();
    }
}
