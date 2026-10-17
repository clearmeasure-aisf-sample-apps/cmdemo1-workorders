using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- discovered via FluentValidation's assembly-scan/DI reflection, not by direct reference.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="WorkOrderBuildingsQuery"/>. The query carries no parameters, so no rules
/// apply; the validator exists so the single-API validation middleware accepts the request.
/// </summary>
public sealed class WorkOrderBuildingsQueryValidator : AbstractValidator<WorkOrderBuildingsQuery>;
