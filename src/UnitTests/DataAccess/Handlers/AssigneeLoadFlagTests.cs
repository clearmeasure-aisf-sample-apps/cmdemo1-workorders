using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.Handlers;

[TestFixture]
public class AssigneeLoadFlagTests
{
    [Test]
    public void ShouldDefaultFlagsToZero()
    {
        var flag = new AssigneeLoadFlag();

        flag.AssigneeId.ShouldBe(Guid.Empty);
        flag.Open.ShouldBe(0);
        flag.InProgress.ShouldBe(0);
        flag.Overdue.ShouldBe(0);
        flag.Completed.ShouldBe(0);
    }
}
