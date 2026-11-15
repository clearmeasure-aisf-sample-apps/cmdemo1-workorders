using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="CreateDatedWorkOrdersCommand"/>: creator, assignee, title and description
/// are required. Due-date batch size is enforced in the handler so in-process callers keep a Failed
/// result instead of <c>ValidationException</c>.
/// </summary>
public sealed class CreateDatedWorkOrdersCommandValidator : AbstractValidator<CreateDatedWorkOrdersCommand>
{
    public CreateDatedWorkOrdersCommandValidator()
    {
        RuleFor(x => x.CreatorUsername).NotEmpty();
        RuleFor(x => x.AssigneeUsername).NotEmpty();
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(WorkOrder.DescriptionMaxLength)
            .WithMessage($"Description cannot exceed {WorkOrder.DescriptionMaxLength} characters.");
        // Due-date batch size stays in CreateDatedWorkOrdersHandler.BatchSizeFailure so in-process
        // IBus callers keep today's Failed result instead of ValidationException (#1804).
    }
}
