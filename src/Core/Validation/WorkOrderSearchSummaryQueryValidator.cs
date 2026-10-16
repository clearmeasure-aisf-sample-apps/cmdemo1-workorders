using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- discovered via FluentValidation's assembly-scan/DI reflection, not by direct reference.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="WorkOrderSearchSummaryQuery"/>. The user name is optional (anonymous
/// visitors get zero user counts) but bounded.
/// </summary>
public sealed class WorkOrderSearchSummaryQueryValidator : AbstractValidator<WorkOrderSearchSummaryQuery>
{
    /// <summary>Creates the validator rules.</summary>
    public WorkOrderSearchSummaryQueryValidator()
    {
        RuleFor(x => x.UserName).MaximumLength(256);
    }
}
