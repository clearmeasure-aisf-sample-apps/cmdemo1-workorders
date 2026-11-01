using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="WorkOrderReportsQuery"/>. This query has no parameters to validate.
/// </summary>
public sealed class WorkOrderReportsQueryValidator : AbstractValidator<WorkOrderReportsQuery>;
