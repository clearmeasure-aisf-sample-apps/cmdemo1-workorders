using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
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

    private static SaveDraftCommand CreateCommand(string? title)
    {
        var creator = new Employee("creator1", "Creator", "One", "creator1@example.com");
        var workOrder = new WorkOrder { Title = title, Creator = creator };
        return new SaveDraftCommand(workOrder, creator);
    }
}
