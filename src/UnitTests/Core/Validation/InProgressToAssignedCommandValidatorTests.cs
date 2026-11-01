using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class InProgressToAssignedCommandValidatorTests
{
    private readonly InProgressToAssignedCommandValidator _validator = new();

    [Test]
    public void ShouldPass_WhenReasonIsOneCharacter()
    {
        _validator.TestValidate(Command("a")).IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenReasonIsAtMaxLength()
    {
        _validator.TestValidate(Command(new string('a', InProgressToAssignedCommand.ReasonMaxLength)))
            .IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenReasonIsNull()
    {
        _validator.TestValidate(Command(null)).ShouldHaveValidationErrorFor(command => command.Reason);
    }

    [Test]
    public void ShouldFail_WhenReasonIsEmpty()
    {
        _validator.TestValidate(Command("")).ShouldHaveValidationErrorFor(command => command.Reason);
    }

    [Test]
    public void ShouldFail_WhenReasonIsWhitespace()
    {
        _validator.TestValidate(Command("   ")).ShouldHaveValidationErrorFor(command => command.Reason);
    }

    [Test]
    public void ShouldFail_WhenReasonExceedsMaxLength()
    {
        _validator.TestValidate(Command(new string('a', InProgressToAssignedCommand.ReasonMaxLength + 1)))
            .ShouldHaveValidationErrorFor(command => command.Reason);
    }

    private static InProgressToAssignedCommand Command(string? reason)
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };
        return new InProgressToAssignedCommand(order, employee) { Reason = reason };
    }
}
