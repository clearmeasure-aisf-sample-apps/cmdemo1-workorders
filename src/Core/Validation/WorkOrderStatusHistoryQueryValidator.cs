using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class WorkOrderStatusHistoryQueryValidator : AbstractValidator<WorkOrderStatusHistoryQuery>
{
    public WorkOrderStatusHistoryQueryValidator()
    {
        RuleFor(x => x.WorkOrderId).NotEmpty();
    }
}
