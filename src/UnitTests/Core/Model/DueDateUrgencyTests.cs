using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class DueDateUrgencyTests
{
    [Test]
    public void None_ShouldHaveNumericCodeZero()
    {
        ((int)DueDateUrgency.None).ShouldBe(0);
    }

    [Test]
    public void DueToday_ShouldHaveNumericCodeOne()
    {
        ((int)DueDateUrgency.DueToday).ShouldBe(1);
    }

    [Test]
    public void Overdue_ShouldHaveNumericCodeTwo()
    {
        ((int)DueDateUrgency.Overdue).ShouldBe(2);
    }
}
