using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.WorkOrders.FieldRules;

/// <summary>
/// Pins every user-visible work-order field message to the wording that shipped before #1805.
/// </summary>
[TestFixture]
public class WorkOrderFieldRulesMessageCharacterizationTests
{
    [Test]
    public void TitleCannotExceed_MatchesPreRefactorWording()
    {
        WorkOrderFieldRules.TitleCannotExceed.ShouldBe("Title cannot exceed 600 characters.");
    }

    [Test]
    public void DescriptionCannotExceed_MatchesPreRefactorWording()
    {
        WorkOrderFieldRules.DescriptionCannotExceed.ShouldBe("Description cannot exceed 4000 characters.");
    }

    [Test]
    public void InstructionsCannotExceed_MatchesPreRefactorWording()
    {
        WorkOrderFieldRules.InstructionsCannotExceed.ShouldBe("Instructions cannot exceed 4000 characters.");
    }

    [Test]
    public void BuildingCannotExceed_MatchesPreRefactorWording()
    {
        WorkOrderFieldRules.BuildingCannotExceed.ShouldBe("Building cannot exceed 100 characters.");
    }

    [Test]
    public void BuildingMustBeAtMost_MatchesCsvImportWording()
    {
        WorkOrderFieldRules.BuildingMustBeAtMost.ShouldBe("Building must be at most 100 characters.");
    }

    [Test]
    public void FloorCannotExceed_MatchesPreRefactorWording()
    {
        WorkOrderFieldRules.FloorCannotExceed.ShouldBe("Floor cannot exceed 20 characters.");
    }

    [Test]
    public void RoomNumberCannotExceed_MatchesPreRefactorWording()
    {
        WorkOrderFieldRules.RoomNumberCannotExceed.ShouldBe("Room number cannot exceed 50 characters.");
    }

    [Test]
    public void ReasonCannotExceed_MatchesPreRefactorWording()
    {
        WorkOrderFieldRules.ReasonCannotExceed.ShouldBe("Reason cannot exceed 200 characters.");
    }

    [Test]
    public void TitleCannotExceed_EmbedsTitleMaxLength()
    {
        WorkOrderFieldRules.TitleCannotExceed.ShouldBe(
            $"Title cannot exceed {WorkOrder.TitleMaxLength} characters.");
    }

    [Test]
    public void DescriptionCannotExceed_EmbedsDescriptionMaxLength()
    {
        WorkOrderFieldRules.DescriptionCannotExceed.ShouldBe(
            $"Description cannot exceed {WorkOrder.DescriptionMaxLength} characters.");
    }

    [Test]
    public void BuildingCannotExceed_EmbedsBuildingMaxLength()
    {
        WorkOrderFieldRules.BuildingCannotExceed.ShouldBe(
            $"Building cannot exceed {WorkOrder.BuildingMaxLength} characters.");
    }

    [Test]
    public void ReasonCannotExceed_EmbedsReasonMaxLength()
    {
        WorkOrderFieldRules.ReasonCannotExceed.ShouldBe(
            $"Reason cannot exceed {InProgressToAssignedCommand.ReasonMaxLength} characters.");
    }

    [Test]
    public void BatchCannotExceed_EmbedsMaximumBatchSize()
    {
        CreateDatedWorkOrdersCommand.BatchCannotExceed.ShouldBe(
            $"A dated work-order batch cannot exceed {CreateDatedWorkOrdersCommand.MaximumBatchSize} dates.");
    }

    [Test]
    public void CreateWorkOrderToolDescription_EmbedsBuildingMaxLength()
    {
        WorkOrderFieldRules.CreateWorkOrderToolDescription.ShouldContain(
            $"max {WorkOrder.BuildingMaxLength} characters");
    }

    [Test]
    public void ShelveReasonRequired_MatchesManagePageWording()
    {
        WorkOrderFieldRules.ShelveReasonRequired.ShouldBe("A reason is required to shelve a work order.");
    }

    [Test]
    public void ShelveRequiresReason_MatchesMcpWording()
    {
        WorkOrderFieldRules.ShelveRequiresReason.ShouldBe("Shelve requires a reason.");
    }

    [Test]
    public void DescriptionRequired_MatchesMcpCreateWording()
    {
        WorkOrderFieldRules.DescriptionRequired.ShouldBe("Description is required.");
    }

    [Test]
    public void TitleFieldRequired_MatchesMcpSaveWording()
    {
        WorkOrderFieldRules.TitleFieldRequired.ShouldBe("The Title field is required.");
    }

    [Test]
    public void WorkOrderNumberRequired_MatchesMcpEditWording()
    {
        WorkOrderFieldRules.WorkOrderNumberRequired.ShouldBe("Work order number is required.");
    }

    [Test]
    public void BatchCannotExceed_MatchesDatedCreateWording()
    {
        CreateDatedWorkOrdersCommand.BatchCannotExceed.ShouldBe(
            "A dated work-order batch cannot exceed 10 dates.");
    }

    [Test]
    public void AtLeastOneDueDateRequired_MatchesDatedCreateWording()
    {
        CreateDatedWorkOrdersCommand.AtLeastOneDueDateRequired.ShouldBe(
            "At least one due date is required.");
    }

    [Test]
    public void DueDateInThePastMessage_MatchesPreRefactorWording()
    {
        WorkOrder.DueDateInThePastMessage.ShouldBe("The due date cannot be in the past.");
    }
}
