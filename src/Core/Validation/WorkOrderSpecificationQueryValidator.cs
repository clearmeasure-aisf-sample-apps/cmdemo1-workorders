using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="WorkOrderSpecificationQuery"/>: bounds the free-text filters and the
/// completed-within window.
/// </summary>
public sealed class WorkOrderSpecificationQueryValidator : AbstractValidator<WorkOrderSpecificationQuery>
{
    /// <summary>Maximum length of <see cref="WorkOrderSpecificationQuery.SearchText"/>.</summary>
    public const int SearchTextMaxLength = 200;

    /// <summary>Largest accepted <see cref="WorkOrderSpecificationQuery.CompletedWithinDays"/>.</summary>
    public const int CompletedWithinDaysMax = 3650;

    /// <summary>Creates the validator rules.</summary>
    public WorkOrderSpecificationQueryValidator()
    {
        RuleFor(x => x.SearchText).MaximumLength(SearchTextMaxLength);
        RuleFor(x => x.Building).MaximumLength(WorkOrder.BuildingMaxLength);
        RuleFor(x => x.Room).MaximumLength(WorkOrder.RoomNumberMaxLength);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.CompletedWithinDays)
            .InclusiveBetween(1, CompletedWithinDaysMax)
            .When(x => x.CompletedWithinDays.HasValue);
    }
}
