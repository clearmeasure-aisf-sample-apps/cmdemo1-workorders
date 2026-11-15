using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Import;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.McpServer.Tools.Arguments;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.WorkOrders.FieldRules;

/// <summary>
/// Pins UI form, MCP, CSV and FluentValidation surfaces to the same Core messages.
/// </summary>
[TestFixture]
public class WorkOrderFieldRulesSurfaceCharacterizationTests
{
    [Test]
    public void UiForm_TitleTooLong_UsesCoreMessage()
    {
        var model = new WorkOrderManageModel
        {
            Title = new string('T', WorkOrder.TitleMaxLength + 1),
            Description = "ok"
        };

        var results = ValidateModel(model);

        results.Single(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)))
            .ErrorMessage.ShouldBe(WorkOrderFieldRules.TitleCannotExceed);
    }

    [Test]
    public void UiForm_RoomNumberTooLong_UsesCoreMessage()
    {
        var model = new WorkOrderManageModel
        {
            Title = "ok",
            Description = "ok",
            OriginalRoomNumber = "1",
            RoomNumber = new string('R', WorkOrder.RoomNumberEntryMaxLength + 1)
        };

        var results = ValidateModel(model);

        results.Single(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.RoomNumber)))
            .ErrorMessage.ShouldBe(WorkOrderFieldRules.RoomNumberCannotExceed);
    }

    [Test]
    public void McpBuildingArgument_TooLong_UsesCoreMessage()
    {
        var building = BuildingArgument.Parse(new string('B', WorkOrder.BuildingMaxLength + 1));

        building.Error.ShouldBe(WorkOrderFieldRules.BuildingCannotExceed);
    }

    [Test]
    public void McpShelveReason_Empty_UsesCoreMcpMessage()
    {
        ShelveReason.For("Shelve", " ").Error.ShouldBe(WorkOrderFieldRules.ShelveRequiresReason);
    }

    [Test]
    public void McpSaveTitlePatch_Blank_UsesCoreMessage()
    {
        WorkOrderFieldRules.TitlePatchError(" ").ShouldBe(WorkOrderFieldRules.TitleFieldRequired);
    }

    [Test]
    public void CsvImport_DescriptionTooLong_UsesCoreMessage()
    {
        var csv = "Title,Description,CreatorUsername\n"
                  + $"T,{new string('D', WorkOrder.DescriptionMaxLength + 1)},u\n";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        var result = WorkOrderBulkImportCsvParser.Parse(stream);

        result.Rows[0].Error.ShouldBe(WorkOrderFieldRules.DescriptionCannotExceed);
    }

    [Test]
    public void SaveDraftValidator_TitleTooLong_UsesCoreMessage()
    {
        var validator = new SaveDraftCommandValidator(TimeProvider.System);
        var command = new SaveDraftCommand(
            new WorkOrder { Title = new string('T', WorkOrder.TitleMaxLength + 1), Description = "d" },
            new Employee());

        validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.WorkOrder.Title)
            .WithErrorMessage(WorkOrderFieldRules.TitleCannotExceed);
    }

    [Test]
    public void ShelveValidator_EmptyReason_UsesCoreMessage()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };
        var command = new InProgressToAssignedCommand(order, employee) { Reason = "" };

        new InProgressToAssignedCommandValidator().TestValidate(command)
            .ShouldHaveValidationErrorFor(c => c.Reason)
            .WithErrorMessage(WorkOrderFieldRules.ShelveReasonRequired);
    }

    private static List<ValidationResult> ValidateModel(WorkOrderManageModel model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, true);
        return results;
    }
}
