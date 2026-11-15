using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="InProgressToAssignedCommand"/>: a shelve reason is required and
/// at most <see cref="InProgressToAssignedCommand.ReasonMaxLength"/> characters.
/// </summary>
public sealed class InProgressToAssignedCommandValidator : AbstractValidator<InProgressToAssignedCommand>
{
    /// <summary>Defines the rules.</summary>
    public InProgressToAssignedCommandValidator()
    {
        RuleFor(command => command.Reason)
            .NotEmpty()
            .WithMessage(WorkOrderFieldRules.ShelveReasonRequired)
            .MaximumLength(InProgressToAssignedCommand.ReasonMaxLength)
            .WithMessage(WorkOrderFieldRules.ReasonCannotExceed);
    }
}
