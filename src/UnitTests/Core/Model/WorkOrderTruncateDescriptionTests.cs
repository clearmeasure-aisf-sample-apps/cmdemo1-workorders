using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderTruncateDescriptionTests
{
    [Test]
    public void TruncateDescription_WhenDescriptionUnderMax_ReturnsUnchanged()
    {
        var description = "Short description";

        var result = WorkOrder.TruncateDescription(description);

        result.ShouldBe(description);
    }

    [Test]
    public void TruncateDescription_WhenDescriptionAtMax_ReturnsUnchanged()
    {
        var description = new string('x', WorkOrder.DescriptionMaxLength);

        var result = WorkOrder.TruncateDescription(description);

        result.ShouldBe(description);
        result.Length.ShouldBe(WorkOrder.DescriptionMaxLength);
    }

    [Test]
    public void TruncateDescription_WhenDescriptionOverMax_TruncatesToMaxLength()
    {
        var description = new string('x', WorkOrder.DescriptionMaxLength + 100);

        var result = WorkOrder.TruncateDescription(description);

        result.Length.ShouldBe(WorkOrder.DescriptionMaxLength);
        result.ShouldBe(new string('x', WorkOrder.DescriptionMaxLength));
    }

    [Test]
    public void ApplyReformat_SetsTitleAndTruncatesDescription()
    {
        var order = new WorkOrder { Title = "old", Description = "old" };
        var longDescription = new string('y', WorkOrder.DescriptionMaxLength + 50);

        order.ApplyReformat("New Title", longDescription);

        order.Title.ShouldBe("New Title");
        order.Description!.Length.ShouldBe(WorkOrder.DescriptionMaxLength);
    }
}
