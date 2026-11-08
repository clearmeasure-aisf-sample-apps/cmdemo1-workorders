using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.UI.Server.Validation;

// ReSharper disable once ClassNeverInstantiated.Global -- registered by DI (FluentValidation assembly scan)
public sealed class EmployeeGetAllQueryValidator : AbstractValidator<EmployeeGetAllQuery>
{
    /// <summary>Creates the validator rules.</summary>
    public EmployeeGetAllQueryValidator()
    {
        RuleFor(query => query.Skip).GreaterThanOrEqualTo(0);
        RuleFor(query => query.Take).InclusiveBetween(1, EmployeeGetAllQuery.MaxTake);
    }
}
