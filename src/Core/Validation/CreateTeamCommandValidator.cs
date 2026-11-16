using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// FluentValidation for <see cref="CreateTeamCommand"/> (#1824): the name is required, and the name
/// and description fit the columns. HTTP uses this via <c>HttpEnvelopeValidatorSet</c>. In-process
/// handlers inject this concrete type (not <c>IValidator&lt;T&gt;</c>) and map failures to Failed
/// results; duplicate-name and unknown-member rules stay on
/// <see cref="CreateTeamCommand.FindRuleViolations"/>.
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
