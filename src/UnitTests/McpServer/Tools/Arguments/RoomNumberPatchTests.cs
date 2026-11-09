using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer.Tools.Arguments;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools.Arguments;

[TestFixture]
public class RoomNumberPatchTests
{
    [TestCase(null, "101")]
    [TestCase("  ", "")]
    [TestCase("204B", "204B")]
    public void ShouldApplyTheRoom_WhenThePatchIsOmittedBlankOrARoom(string? text, string expected)
    {
        var workOrder = WorkOrderToolMother.Draft(WorkOrderToolMother.Creator());

        new RoomNumberPatch(text).ApplyTo(workOrder);

        workOrder.RoomNumber.ShouldBe(expected);
    }

    [Test]
    public void ShouldCutTheStoredRoom_WhenItIsLongerThanTheColumn()
    {
        var workOrder = WorkOrderToolMother.Draft(WorkOrderToolMother.Creator());

        new RoomNumberPatch(new string('r', WorkOrder.RoomNumberMaxLength + 5)).ApplyTo(workOrder);

        workOrder.RoomNumber.ShouldBe(new string('r', WorkOrder.RoomNumberMaxLength));
    }

    [Test]
    public void ShouldRejectTheEntry_WhenAChangedRoomIsLongerThanTheEntryMaximum()
    {
        var workOrder = WorkOrderToolMother.Draft(WorkOrderToolMother.Creator());

        var error = new RoomNumberPatch(new string('r', WorkOrder.RoomNumberEntryMaxLength + 1)).ErrorFor(workOrder);

        error.ShouldBe($"Room number cannot exceed {WorkOrder.RoomNumberEntryMaxLength} characters.");
    }

    [Test]
    public void ShouldAcceptTheEntry_WhenItResendsTheCurrentRoom()
    {
        var workOrder = WorkOrderToolMother.Draft(WorkOrderToolMother.Creator());
        workOrder.RoomNumber = new string('r', WorkOrder.RoomNumberEntryMaxLength + 1);

        var error = new RoomNumberPatch(workOrder.RoomNumber).ErrorFor(workOrder);

        error.ShouldBeNull();
    }
}
