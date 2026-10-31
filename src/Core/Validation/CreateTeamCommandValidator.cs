using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="CreateTeamCommand"/>: the name is required, and the name and description fit the
/// columns. Rules that depend on stored data are in <see cref="CreateTeamCommand.FindRuleViolations"/>.
/// </summary>
public sealed class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    /// <summary>
    /// Defines the rules.
    /// </summary>
    public CreateTeamCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Team.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(Team.DescriptionMaxLength);
        RuleFor(x => x.MemberUserNames).NotNull();
        RuleForEach(x => x.MemberUserNames).NotEmpty();
    }
}
