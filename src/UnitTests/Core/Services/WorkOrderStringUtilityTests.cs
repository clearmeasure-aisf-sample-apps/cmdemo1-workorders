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
        string description = "Short description";

        string result = WorkOrderStringUtility.TruncateDescription(description);

        result.ShouldBe(description);
    }

    [Test]
    public void TruncateDescription_WhenDescriptionAt4000_ReturnsUnchanged()
    {
        string description = new('x', WorkOrder.DescriptionMaxLength);

        string result = WorkOrderStringUtility.TruncateDescription(description);

        result.ShouldBe(description);
        result.Length.ShouldBe(WorkOrder.DescriptionMaxLength);
    }

    [Test]
    public void TruncateDescription_WhenDescriptionOver4000_TruncatesToMaxLength()
    {
        string description = new('x', WorkOrder.DescriptionMaxLength + 100);

        string result = WorkOrderStringUtility.TruncateDescription(description);

        result.Length.ShouldBe(WorkOrder.DescriptionMaxLength);
        result.ShouldBe(new string('x', WorkOrder.DescriptionMaxLength));
    }
}
