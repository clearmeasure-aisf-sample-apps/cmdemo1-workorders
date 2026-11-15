using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.WorkOrders.FieldRules;

[TestFixture]
public class WorkOrderFieldRulesTests
{
    [Test]
    public void TitleLengthError_ReturnsNull_WhenAtMaxLength()
    {
        var title = new string('T', WorkOrder.TitleMaxLength);

        WorkOrderFieldRules.TitleLengthError(title).ShouldBeNull();
    }

    [Test]
    public void TitleLengthError_ReturnsMessage_WhenOverMaxLength()
    {
        var title = new string('T', WorkOrder.TitleMaxLength + 1);

        WorkOrderFieldRules.TitleLengthError(title).ShouldBe(WorkOrderFieldRules.TitleCannotExceed);
    }

    [Test]
    public void DescriptionRequiredOrLengthError_ReturnsRequired_WhenBlank()
    {
        WorkOrderFieldRules.DescriptionRequiredOrLengthError("  ")
            .ShouldBe(WorkOrderFieldRules.DescriptionRequired);
    }

    [Test]
    public void DescriptionRequiredOrLengthError_ReturnsLength_WhenTooLong()
    {
        var description = new string('D', WorkOrder.DescriptionMaxLength + 1);

        WorkOrderFieldRules.DescriptionRequiredOrLengthError(description)
            .ShouldBe(WorkOrderFieldRules.DescriptionCannotExceed);
    }

    [Test]
    public void BuildingCsvLengthError_UsesCsvWording_WhenTooLong()
    {
        var building = new string('B', WorkOrder.BuildingMaxLength + 1);

        WorkOrderFieldRules.BuildingCsvLengthError(building)
            .ShouldBe(WorkOrderFieldRules.BuildingMustBeAtMost);
    }

    [Test]
    public void RoomNumberEntryLengthError_ReturnsNull_WhenUnchangedLegacyRoom()
    {
        var legacy = new string('R', WorkOrder.RoomNumberEntryMaxLength + 10);

        WorkOrderFieldRules.RoomNumberEntryLengthError(legacy, legacy).ShouldBeNull();
    }

    [Test]
    public void RoomNumberEntryLengthError_ReturnsMessage_WhenChangedAndTooLong()
    {
        var previous = "12";
        var next = new string('R', WorkOrder.RoomNumberEntryMaxLength + 1);

        WorkOrderFieldRules.RoomNumberEntryLengthError(next, previous)
            .ShouldBe(WorkOrderFieldRules.RoomNumberCannotExceed);
    }

    [Test]
    public void ShelveReasonError_ReturnsRequired_WhenEmpty()
    {
        WorkOrderFieldRules.ShelveReasonError("").ShouldBe(WorkOrderFieldRules.ShelveReasonRequired);
    }

    [Test]
    public void ShelveReasonError_ReturnsLength_WhenTooLong()
    {
        var reason = new string('r', InProgressToAssignedCommand.ReasonMaxLength + 1);

        WorkOrderFieldRules.ShelveReasonError(reason).ShouldBe(WorkOrderFieldRules.ReasonCannotExceed);
    }

    [Test]
    public void McpShelveReasonError_ReturnsMcpRequired_WhenEmpty()
    {
        WorkOrderFieldRules.McpShelveReasonError("").ShouldBe(WorkOrderFieldRules.ShelveRequiresReason);
    }

    [Test]
    public void TitlePatchError_ReturnsFieldRequired_WhenBlankPatch()
    {
        WorkOrderFieldRules.TitlePatchError("  ").ShouldBe(WorkOrderFieldRules.TitleFieldRequired);
    }

    [Test]
    public void TitlePatchError_ReturnsNull_WhenTitleOmitted()
    {
        WorkOrderFieldRules.TitlePatchError(null).ShouldBeNull();
    }
}
