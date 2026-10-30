using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class WorkOrderStringUtilityTests
{
    [Test]
    public void TruncateDescription_WhenDescriptionUnder4000_ReturnsUnchanged()
    {
        var description = "Short description";

        var result = WorkOrderStringUtility.TruncateDescription(description);

        result.ShouldBe(description);
    }

    [Test]
    public void TruncateDescription_WhenDescriptionAt4000_ReturnsUnchanged()
    {
        var description = new string('x', WorkOrder.DescriptionMaxLength);

        var result = WorkOrderStringUtility.TruncateDescription(description);

        result.ShouldBe(description);
        result.Length.ShouldBe(WorkOrder.DescriptionMaxLength);
    }

    [Test]
    public void TruncateDescription_WhenDescriptionOver4000_TruncatesToMaxLength()
    {
        var description = new string('x', WorkOrder.DescriptionMaxLength + 100);

        var result = WorkOrderStringUtility.TruncateDescription(description);

        result.Length.ShouldBe(WorkOrder.DescriptionMaxLength);
        result.ShouldBe(new string('x', WorkOrder.DescriptionMaxLength));
    }
}
