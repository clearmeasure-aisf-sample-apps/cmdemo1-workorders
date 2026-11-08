using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- discovered via FluentValidation's assembly-scan/DI reflection, not by direct reference.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Remoting envelope requires a registered FluentValidation validator for every payload type.
/// </summary>
public sealed class WorkOrdersByNumbersQueryValidator : AbstractValidator<WorkOrdersByNumbersQuery>
{
    /// <summary>Creates the validator rules.</summary>
    public WorkOrdersByNumbersQueryValidator()
    {
        RuleFor(query => query.Numbers)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(numbers => numbers.Length <= WorkOrdersByNumbersQuery.MaxNumbers)
            .WithMessage($"At most {WorkOrdersByNumbersQuery.MaxNumbers} numbers.");
    }
}
