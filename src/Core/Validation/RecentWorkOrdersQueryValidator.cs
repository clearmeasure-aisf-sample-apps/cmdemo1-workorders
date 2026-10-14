using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- discovered via FluentValidation's assembly-scan/DI reflection, not by direct reference.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="RecentWorkOrdersQuery"/>: the count is between 1 and 50.
/// </summary>
public sealed class RecentWorkOrdersQueryValidator : AbstractValidator<RecentWorkOrdersQuery>
{
    /// <summary>Creates the validator rules.</summary>
    public RecentWorkOrdersQueryValidator()
    {
        RuleFor(q => q.Count).InclusiveBetween(1, 50);
    }
}
