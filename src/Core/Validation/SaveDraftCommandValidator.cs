using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="SaveDraftCommand"/>: rejects a work order title longer than
/// <see cref="WorkOrder.TitleMaxLength"/>, a changed room number longer than
/// <see cref="WorkOrder.RoomNumberEntryMaxLength"/>, a building longer than
/// <see cref="WorkOrder.BuildingMaxLength"/>, a floor longer than
/// <see cref="WorkOrder.FloorMaxLength"/> and a newly set due date earlier than today
/// in America/Chicago before it reaches persistence.
/// </summary>
public sealed class SaveDraftCommandValidator : AbstractValidator<SaveDraftCommand>
{
    public SaveDraftCommandValidator()
        : this(TimeProvider.System)
    {
    }

    public SaveDraftCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.WorkOrder.Title)
            .MaximumLength(WorkOrder.TitleMaxLength)
            .WithMessage($"Title cannot exceed {WorkOrder.TitleMaxLength} characters.");

        RuleFor(x => x.WorkOrder.Description)
            .MaximumLength(WorkOrder.DescriptionMaxLength)
            .WithMessage($"Description cannot exceed {WorkOrder.DescriptionMaxLength} characters.");

        RuleFor(x => x.WorkOrder.RoomNumber)
            .MaximumLength(WorkOrder.RoomNumberEntryMaxLength)
            .WithMessage($"Room number cannot exceed {WorkOrder.RoomNumberEntryMaxLength} characters.")
            .When(x => x.WorkOrder.RoomNumber != x.PreviousRoomNumber);

        RuleFor(x => x.WorkOrder.Building)
            .MaximumLength(WorkOrder.BuildingMaxLength)
            .WithMessage($"Building cannot exceed {WorkOrder.BuildingMaxLength} characters.");

        RuleFor(x => x.WorkOrder.Floor)
            .MaximumLength(WorkOrder.FloorMaxLength)
            .WithMessage($"Floor cannot exceed {WorkOrder.FloorMaxLength} characters.");

        RuleFor(x => x.WorkOrder.DueDate)
            .Must((command, dueDate) => !WorkOrder.IsNewlySetPastDueDate(
                dueDate,
                command.PreviousDueDate,
                ChurchTimeZone.Today(timeProvider)))
            .WithMessage(WorkOrder.DueDateInThePastMessage);
    }
}
