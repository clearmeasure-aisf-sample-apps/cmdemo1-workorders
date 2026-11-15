using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderCountRowTests
{
    [Test]
    public void ShouldDefaultKeysToEmpty_AndFlagsToZero()
    {
        var row = new WorkOrderCountRow();

        row.Building.ShouldBe("");
        row.Room.ShouldBe("");
        row.Open.ShouldBe(0);
        row.Overdue.ShouldBe(0);
        row.Completed.ShouldBe(0);
        row.Number.ShouldBeNull();
    }
}
