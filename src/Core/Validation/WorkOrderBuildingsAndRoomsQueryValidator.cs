using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

public class WorkOrderBuildingsAndRoomsQueryValidator : AbstractValidator<WorkOrderBuildingsAndRoomsQuery>
{
    public WorkOrderBuildingsAndRoomsQueryValidator()
    {
        // No validation rules needed for this simple query
    }
}
