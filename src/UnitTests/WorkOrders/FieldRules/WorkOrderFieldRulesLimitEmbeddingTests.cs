using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.WorkOrders.FieldRules;

/// <summary>
/// Ties each hard-coded number in a Core field message to its max-length constant.
/// </summary>
[TestFixture]
public class WorkOrderFieldRulesLimitEmbeddingTests
{
    [Test]
    public void FloorCannotExceed_EmbedsFloorMaxLength()
    {
        WorkOrderFieldRules.FloorCannotExceed.ShouldBe(
            $"Floor cannot exceed {WorkOrder.FloorMaxLength} characters.");
    }

    [Test]
    public void RoomNumberCannotExceed_EmbedsRoomNumberEntryMaxLength()
    {
        WorkOrderFieldRules.RoomNumberCannotExceed.ShouldBe(
            $"Room number cannot exceed {WorkOrder.RoomNumberEntryMaxLength} characters.");
    }

    [Test]
    public void InstructionsCannotExceed_EmbedsInstructionsMaxLength()
    {
        WorkOrderFieldRules.InstructionsCannotExceed.ShouldBe(
            $"Instructions cannot exceed {WorkOrder.InstructionsMaxLength} characters.");
    }

    [Test]
    public void BuildingMustBeAtMost_EmbedsBuildingMaxLength()
    {
        WorkOrderFieldRules.BuildingMustBeAtMost.ShouldBe(
            $"Building must be at most {WorkOrder.BuildingMaxLength} characters.");
    }

    [Test]
    public void BuildingParameterDescription_EmbedsBuildingMaxLength()
    {
        WorkOrderFieldRules.BuildingParameterDescription.ShouldBe(
            $"Optional building name; max {WorkOrder.BuildingMaxLength} characters");
    }

    [Test]
    public void BuildingParameterClearDescription_EmbedsBuildingMaxLength()
    {
        WorkOrderFieldRules.BuildingParameterClearDescription.ShouldBe(
            $"Optional building name; max {WorkOrder.BuildingMaxLength} characters; empty clears");
    }

    [Test]
    public void UpdateWorkOrderToolDescription_EmbedsBuildingMaxLength()
    {
        WorkOrderFieldRules.UpdateWorkOrderToolDescription.ShouldContain(
            $"Building: max {WorkOrder.BuildingMaxLength} characters.");
    }

    [Test]
    public void ExecuteWorkOrderCommandToolDescription_EmbedsReasonMaxLength()
    {
        WorkOrderFieldRules.ExecuteWorkOrderCommandToolDescription.ShouldContain(
            $"at most {InProgressToAssignedCommand.ReasonMaxLength} characters.");
    }

    [Test]
    public void ShelveReasonParameterDescription_EmbedsReasonMaxLength()
    {
        WorkOrderFieldRules.ShelveReasonParameterDescription.ShouldBe(
            $"Reason for shelving (required for Shelve and InProgressToAssignedCommand, at most {InProgressToAssignedCommand.ReasonMaxLength} characters)");
    }
}
