using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderPriorityExtensionsTests
{
    [TestCase(WorkOrderPriority.Low, "Low")]
    [TestCase(WorkOrderPriority.Medium, "Medium")]
    [TestCase(WorkOrderPriority.High, "High")]
    public void ToLabel_ShouldReturnHumanReadableLabel(WorkOrderPriority priority, string expectedLabel)
    {
        priority.ToLabel().ShouldBe(expectedLabel);
    }

    [Test]
    public void ToLabel_WhenPriorityIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var undefined = (WorkOrderPriority)99;

        Should.Throw<ArgumentOutOfRangeException>(() => undefined.ToLabel());
    }
}
