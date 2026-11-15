using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

// ReSharper disable once ClassNeverInstantiated.Global -- registered by DI (FluentValidation assembly scan)
/// <summary>
/// Validator for <see cref="EmployeeGetAllQuery"/>: <see cref="EmployeeGetAllQuery.Skip"/> is non-negative
/// and <see cref="EmployeeGetAllQuery.Take"/> stays within 1..<see cref="EmployeeGetAllQuery.MaxTake"/>.
/// </summary>
public sealed class EmployeeGetAllQueryValidator : AbstractValidator<EmployeeGetAllQuery>
{
    /// <summary>Creates the validator rules.</summary>
    public EmployeeGetAllQueryValidator()
    {
        RuleFor(query => query.Skip).GreaterThanOrEqualTo(0);
        RuleFor(query => query.Take).InclusiveBetween(1, EmployeeGetAllQuery.MaxTake);
    }
}
