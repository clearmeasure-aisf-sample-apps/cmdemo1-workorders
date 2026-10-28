using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WorkOrderStatusHistoryQueryValidatorTests
{
    private WorkOrderStatusHistoryQueryValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new WorkOrderStatusHistoryQueryValidator();
    }

    [Test]
    public void ShouldPass_WhenWorkOrderIdIsProvided()
    {
        var query = new WorkOrderStatusHistoryQuery(Guid.NewGuid());

        var result = _validator.TestValidate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenWorkOrderIdIsEmpty()
    {
        var query = new WorkOrderStatusHistoryQuery(Guid.Empty);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.WorkOrderId);
    }
}
