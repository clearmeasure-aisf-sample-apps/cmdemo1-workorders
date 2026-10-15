using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WorkOrderStatusHistoryQueryValidatorTests
{
    private readonly WorkOrderStatusHistoryQueryValidator _validator = new();

    [Test]
    public void ShouldPass_WhenWorkOrderIdIsSet()
    {
        var result = _validator.TestValidate(new WorkOrderStatusHistoryQuery(Guid.NewGuid()));

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenWorkOrderIdIsEmpty()
    {
        var result = _validator.TestValidate(new WorkOrderStatusHistoryQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.WorkOrderId);
    }
}
