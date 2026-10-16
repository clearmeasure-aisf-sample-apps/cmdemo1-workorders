using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class SaveDraftCommandValidatorTests
{
    private SaveDraftCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new SaveDraftCommandValidator();
    }

    [Test]
    public void ShouldPass_WhenTitleIsAtMaxLength()
    {
        var command = CreateCommand(new string('T', WorkOrder.TitleMaxLength));

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenTitleExceedsMaxLength()
    {
        var command = CreateCommand(new string('T', WorkOrder.TitleMaxLength + 1));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.WorkOrder.Title)
            .WithErrorMessage("Title cannot exceed 300 characters.");
    }

    [Test]
    public void ShouldPass_WhenTitleIsNull()
    {
        var command = CreateCommand(null);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenDescriptionIsAtMaxLength()
    {
        var creator = new Employee("creator1", "Creator", "One", "creator1@example.com");
        var command = new SaveDraftCommand(
            new WorkOrder
            {
                Title = "T",
                Description = new string('D', WorkOrder.DescriptionMaxLength),
                Creator = creator
            },
            creator);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenDescriptionExceedsMaxLength()
    {
        var creator = new Employee("creator1", "Creator", "One", "creator1@example.com");
        var command = new SaveDraftCommand(
            new WorkOrder
            {
                Title = "T",
                Description = new string('D', WorkOrder.DescriptionMaxLength + 1),
                Creator = creator
            },
            creator);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "WorkOrder.Description");
    }

    [Test]
    public void ShouldPass_WhenDescriptionIsNull()
    {
        var creator = new Employee("creator1", "Creator", "One", "creator1@example.com");
        var command = new SaveDraftCommand(
            new WorkOrder { Title = "T", Description = null, Creator = creator },
            creator);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenRoomNumberIsAtEntryMaxLength()
    {
        var roomNumber = new string('R', WorkOrder.RoomNumberEntryMaxLength);
        var command = new SaveDraftCommand(
            new WorkOrder { Title = "T", RoomNumber = roomNumber, Creator = new Employee("c", "C", "O", "c@e.com") },
            new Employee("c", "C", "O", "c@e.com"))
        { PreviousRoomNumber = null };

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenRoomNumberExceedsEntryMaxLength()
    {
        var roomNumber = new string('R', WorkOrder.RoomNumberEntryMaxLength + 1);
        var command = new SaveDraftCommand(
            new WorkOrder { Title = "T", RoomNumber = roomNumber, Creator = new Employee("c", "C", "O", "c@e.com") },
            new Employee("c", "C", "O", "c@e.com"))
        { PreviousRoomNumber = null };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.WorkOrder.RoomNumber)
            .WithErrorMessage("Room number cannot exceed 50 characters.");
    }

    [Test]
    public void ShouldPass_WhenRoomNumberUnchangedAndLong()
    {
        var longRoomNumber = new string('R', WorkOrder.RoomNumberMaxLength);
        var command = new SaveDraftCommand(
            new WorkOrder { Title = "T", RoomNumber = longRoomNumber, Creator = new Employee("c", "C", "O", "c@e.com") },
            new Employee("c", "C", "O", "c@e.com"))
        { PreviousRoomNumber = longRoomNumber };

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenFormSaveChangesRoomNumberBeyondEntryMaxLength()
    {
        var creator = new Employee("c", "C", "O", "c@e.com");
        var workOrder = new WorkOrder
        {
            Title = "T",
            RoomNumber = "A",
            Status = WorkOrderStatus.Draft,
            Creator = creator
        };
        workOrder.RoomNumber = new string('R', WorkOrder.RoomNumberEntryMaxLength + 1);

        var command = (SaveDraftCommand)SaveDraftCommand.WithPreviousRoomNumber(
            new StateCommandList().GetMatchingCommand(workOrder, creator, SaveDraftCommand.Name),
            "A");

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeFalse();
        result.ShouldHaveValidationErrorFor(x => x.WorkOrder.RoomNumber)
            .WithErrorMessage("Room number cannot exceed 50 characters.");
    }

    [Test]
    public void ShouldPass_WhenBuildingIsAtMaxLength()
    {
        var command = CreateCommandWithBuilding(new string('B', WorkOrder.BuildingMaxLength));

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenBuildingExceedsMaxLength()
    {
        var command = CreateCommandWithBuilding(new string('B', WorkOrder.BuildingMaxLength + 1));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.WorkOrder.Building)
            .WithErrorMessage("Building cannot exceed 100 characters.");
    }

    [Test]
    public void ShouldPass_WhenBuildingIsNull()
    {
        var command = CreateCommandWithBuilding(null);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenBuildingIsEmpty()
    {
        var command = CreateCommandWithBuilding(string.Empty);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    private static SaveDraftCommand CreateCommand(string? title)
    {
        var creator = new Employee("creator1", "Creator", "One", "creator1@example.com");
        var workOrder = new WorkOrder { Title = title, Creator = creator };
        return new SaveDraftCommand(workOrder, creator);
    }

    private static SaveDraftCommand CreateCommandWithBuilding(string? building)
    {
        var creator = new Employee("creator1", "Creator", "One", "creator1@example.com");
        var workOrder = new WorkOrder { Title = "T", Building = building, Creator = creator };
        return new SaveDraftCommand(workOrder, creator);
    }
}
