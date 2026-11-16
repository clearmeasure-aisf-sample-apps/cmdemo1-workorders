using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;
public class WorkOrderPriorityExtensionsTests
{
    [Test]
    [Arguments(WorkOrderPriority.Low, "Low")]
    [Arguments(WorkOrderPriority.Medium, "Medium")]
    [Arguments(WorkOrderPriority.High, "High")]
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

