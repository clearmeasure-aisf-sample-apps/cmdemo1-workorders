using ClearMeasure.Bootcamp.McpServer.Tools.Arguments;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools.Arguments;

[TestFixture]
public class DueDatePatchTests
{
    [Test]
    public void ShouldLeaveTheDueDate_WhenThePatchIsOmitted()
    {
        var workOrder = WorkOrderToolMother.Draft(WorkOrderToolMother.Creator());

        new DueDatePatch(null).ApplyTo(workOrder);

        workOrder.DueDate.ShouldBe(new DateOnly(2026, 12, 1));
    }

    [TestCase("")]
    [TestCase("  ")]
    public void ShouldClearTheDueDate_WhenThePatchIsBlank(string text)
    {
        var workOrder = WorkOrderToolMother.Draft(WorkOrderToolMother.Creator());

        new DueDatePatch(text).ApplyTo(workOrder);

        workOrder.DueDate.ShouldBeNull();
    }

    [Test]
    public void ShouldSetTheDueDate_WhenThePatchIsADate()
    {
        var workOrder = WorkOrderToolMother.Draft(WorkOrderToolMother.Creator());

        new DueDatePatch(" 2027-01-15 ").ApplyTo(workOrder);

        workOrder.DueDate.ShouldBe(new DateOnly(2027, 1, 15));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("2027-01-15")]
    public void ShouldReportNoError_WhenThePatchIsOmittedBlankOrADate(string? text)
    {
        var error = new DueDatePatch(text).Error;

        error.ShouldBeNull();
    }

    [Test]
    public void ShouldAskForTheDateFormat_WhenThePatchIsNotADate()
    {
        var error = new DueDatePatch("01/15/2027").Error;

        error.ShouldBe("Invalid due date '01/15/2027'. Use yyyy-MM-dd.");
    }
}
