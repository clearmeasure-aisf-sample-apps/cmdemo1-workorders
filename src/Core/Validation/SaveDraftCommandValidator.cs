using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="SaveDraftCommand"/>: rejects a work order title longer than
/// <see cref="WorkOrder.TitleMaxLength"/> before it reaches persistence.
/// </summary>
public sealed class SaveDraftCommandValidator : AbstractValidator<SaveDraftCommand>
{
    public SaveDraftCommandValidator()
    {
        RuleFor(x => x.WorkOrder.Title)
            .MaximumLength(WorkOrder.TitleMaxLength)
            .WithMessage($"Title cannot exceed {WorkOrder.TitleMaxLength} characters.");
    }
}
