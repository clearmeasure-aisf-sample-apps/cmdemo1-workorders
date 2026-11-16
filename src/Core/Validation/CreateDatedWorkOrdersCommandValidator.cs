using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// FluentValidation for <see cref="CreateDatedWorkOrdersCommand"/> (#1824). HTTP uses this via
/// <c>HttpEnvelopeValidatorSet</c>. In-process handlers inject this concrete type (not
/// <c>IValidator&lt;T&gt;</c>) for shape rules and map failures to Failed results; DueDates empty /
/// batch-size wording stays on <see cref="CreateDatedWorkOrdersCommand.FindRuleViolations"/>.
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
            .WithMessage(WorkOrderFieldRules.DescriptionCannotExceed);
        RuleFor(x => x.DueDates)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(dates => dates.Count <= CreateDatedWorkOrdersCommand.MaximumBatchSize)
            .WithMessage($"At most {CreateDatedWorkOrdersCommand.MaximumBatchSize} due dates are allowed.");
    }
}
