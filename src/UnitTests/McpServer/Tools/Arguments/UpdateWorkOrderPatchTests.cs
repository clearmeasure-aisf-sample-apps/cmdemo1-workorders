using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.McpServer.Tools.Arguments;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools.Arguments;

[TestFixture]
public class UpdateWorkOrderPatchTests
{
    [Test]
    public void ShouldChangeThePlanningFields_WhenAppliedToADraft()
    {
        var draft = WorkOrderToolMother.Draft(WorkOrderToolMother.Creator());
        var patch = new UpdateWorkOrderPatch("high", "  East Wing ", new RoomNumberPatch("204"), new DueDatePatch(""));

        patch.ApplyTo(draft);

        draft.Priority.ShouldBe(WorkOrderPriority.High);
        draft.Building.ShouldBe("East Wing");
        draft.RoomNumber.ShouldBe("204");
        draft.DueDate.ShouldBeNull();
    }

    [Test]
    public void ShouldRecordTheReplacedDueDate_WhenAppliedAsASave()
    {
        var creator = WorkOrderToolMother.Creator();
        var draft = WorkOrderToolMother.Draft(creator);
        var patch = new UpdateWorkOrderPatch("Low", null, new RoomNumberPatch(null), new DueDatePatch("2027-02-01"));

        var save = patch.ApplyAsSave(new WorkOrderRequestContext(draft, creator));

        save.PreviousDueDate.ShouldBe(new DateOnly(2026, 12, 1));
        save.WorkOrder.DueDate.ShouldBe(new DateOnly(2027, 2, 1));
        save.WorkOrder.Priority.ShouldBe(WorkOrderPriority.Low);
    }

    [Test]
    public void ShouldLeaveEveryField_WhenEveryPatchIsOmitted()
    {
        var draft = WorkOrderToolMother.Draft(WorkOrderToolMother.Creator());
        var patch = new UpdateWorkOrderPatch(null, null, new RoomNumberPatch(null), new DueDatePatch(null));

        patch.ApplyTo(draft);

        draft.Priority.ShouldBe(WorkOrderPriority.Medium);
        draft.Building.ShouldBe("Main Hall");
        draft.RoomNumber.ShouldBe("101");
        draft.DueDate.ShouldBe(new DateOnly(2026, 12, 1));
    }

    [TestCase(" ", null, null, "Priority cannot be cleared; omit it to leave unchanged.")]
    [TestCase("Urgent", null, null, "Invalid priority 'Urgent'. Valid values are: Low, Medium, High.")]
    [TestCase(null, null, "soon", "Invalid due date 'soon'. Use yyyy-MM-dd.")]
    public void ShouldRejectThePatch_WhenAFieldBreaksItsRule(string? priority, string? building, string? dueDate, string expected)
    {
        var creator = WorkOrderToolMother.Creator();
        var draft = WorkOrderToolMother.Draft(creator);
        var patch = new UpdateWorkOrderPatch(priority, building, new RoomNumberPatch(null), new DueDatePatch(dueDate));

        var error = patch.ErrorFor(draft, creator);

        error.ShouldBe(expected);
    }

    [Test]
    public void ShouldRejectThePatch_WhenTheBuildingIsTooLong()
    {
        var creator = WorkOrderToolMother.Creator();
        var draft = WorkOrderToolMother.Draft(creator);
        var patch = new UpdateWorkOrderPatch(null, new string('b', WorkOrder.BuildingMaxLength + 1), new RoomNumberPatch(null), new DueDatePatch(null));

        var error = patch.ErrorFor(draft, creator);

        error.ShouldBe("Building cannot exceed 100 characters.");
    }

    [Test]
    public void ShouldDenyThePatch_WhenSomeoneOtherThanTheCreatorUpdates()
    {
        var draft = WorkOrderToolMother.Draft(WorkOrderToolMother.Creator());
        var patch = new UpdateWorkOrderPatch("Low", null, new RoomNumberPatch(null), new DueDatePatch(null));

        var error = patch.ErrorFor(draft, WorkOrderToolMother.OtherEmployee());

        error.ShouldBe("Only the creator 'jsmith' can update draft work order 'WO-1'.");
    }
}
