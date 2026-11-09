using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WorkOrdersByNumbersQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenTheListIsWithinTheCap()
    {
        var validator = new WorkOrdersByNumbersQueryValidator();

        var result = validator.TestValidate(new WorkOrdersByNumbersQuery(["A", "B"]));

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenNumbersIsNull()
    {
        var validator = new WorkOrdersByNumbersQueryValidator();

        var result = validator.TestValidate(new WorkOrdersByNumbersQuery(null!));

        result.ShouldHaveValidationErrorFor(query => query.Numbers);
    }

    [Test]
    public void ShouldFail_WhenTheListExceedsTheCap()
    {
        var validator = new WorkOrdersByNumbersQueryValidator();
        var numbers = Enumerable.Range(0, WorkOrdersByNumbersQuery.MaxNumbers + 1)
            .Select(index => index.ToString())
            .ToArray();

        var result = validator.TestValidate(new WorkOrdersByNumbersQuery(numbers));

        result.ShouldHaveValidationErrorFor(query => query.Numbers);
    }
}
