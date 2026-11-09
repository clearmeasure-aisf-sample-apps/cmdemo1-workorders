using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.McpServer.Tools.Arguments;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools.Arguments;

[TestFixture]
public class SaveWorkOrderPatchTests
{
    [Test]
    public void ShouldChangeOnlyThePatchedFields_WhenAppliedToADraft()
    {
        var draft = WorkOrderToolMother.Draft(WorkOrderToolMother.Creator());
        var patch = Patch(title: "Fix the chiller", instructions: " ");

        patch.ApplyTo(draft);

        draft.Title.ShouldBe("Fix the chiller");
        draft.Instructions.ShouldBe("");
        draft.Description.ShouldBe("The boiler is cold.");
        draft.RoomNumber.ShouldBe("101");
        draft.DueDate.ShouldBe(new DateOnly(2026, 12, 1));
    }

    [Test]
    public void ShouldRecordTheReplacedRoomAndDueDate_WhenAppliedAsASave()
    {
        var creator = WorkOrderToolMother.Creator();
        var draft = WorkOrderToolMother.Draft(creator);
        var patch = new SaveWorkOrderPatch(null, null, null, new RoomNumberPatch("204"), new DueDatePatch("2027-02-01"));

        var save = patch.ApplyAsSave(new WorkOrderRequestContext(draft, creator));

        save.PreviousRoomNumber.ShouldBe("101");
        save.PreviousDueDate.ShouldBe(new DateOnly(2026, 12, 1));
        save.WorkOrder.RoomNumber.ShouldBe("204");
        save.CurrentUser.ShouldBe(creator);
    }

    [Test]
    public void ShouldReportNoError_WhenTheCreatorSavesAValidPatch()
    {
        var creator = WorkOrderToolMother.Creator();
        var draft = WorkOrderToolMother.Draft(creator);

        var error = Patch(title: "Fix the chiller", dueDate: "2027-02-01").ErrorFor(draft, creator);

        error.ShouldBeNull();
    }

    private static IEnumerable<TestCaseData> BrokenTitlesAndDescriptions() =>
    [
        new(" ", null, "The Title field is required."),
        new(new string('t', WorkOrder.TitleMaxLength + 1), null, "Title cannot exceed 300 characters."),
        new(null, new string('d', WorkOrder.DescriptionMaxLength + 1), "Description cannot exceed 4000 characters.")
    ];

    [TestCaseSource(nameof(BrokenTitlesAndDescriptions))]
    public void ShouldRejectThePatch_WhenTitleOrDescriptionBreaksItsRule(string? title, string? description, string expected)
    {
        var creator = WorkOrderToolMother.Creator();
        var draft = WorkOrderToolMother.Draft(creator);

        var error = Patch(title: title, description: description).ErrorFor(draft, creator);

        error.ShouldBe(expected);
    }

    [Test]
    public void ShouldRejectThePatch_WhenTheDueDateIsNotADate()
    {
        var creator = WorkOrderToolMother.Creator();
        var draft = WorkOrderToolMother.Draft(creator);

        var error = Patch(dueDate: "tomorrow").ErrorFor(draft, creator);

        error.ShouldBe("Invalid due date 'tomorrow'. Use yyyy-MM-dd.");
    }

    [Test]
    public void ShouldDenyThePatch_WhenTheWorkOrderIsNoLongerADraft()
    {
        var creator = WorkOrderToolMother.Creator();
        var assigned = WorkOrderToolMother.Assigned(creator);

        var error = Patch(title: "Fix the chiller").ErrorFor(assigned, creator);

        error.ShouldStartWith("Command 'Save' cannot be executed.");
    }

    private static SaveWorkOrderPatch Patch(
        string? title = null,
        string? description = null,
        string? instructions = null,
        string? dueDate = null) =>
        new(title, description, instructions, new RoomNumberPatch(null), new DueDatePatch(dueDate));
}
