using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class RecentWorkOrdersQueryValidatorTests
{
    [TestCase(1)]
    [TestCase(10)]
    [TestCase(50)]
    public void ShouldPass_WhenCountInRange(int count)
    {
        var validator = new RecentWorkOrdersQueryValidator();

        var result = validator.TestValidate(new RecentWorkOrdersQuery(count));

        result.IsValid.ShouldBeTrue();
    }

    [TestCase(0)]
    [TestCase(51)]
    [TestCase(-1)]
    public void ShouldFail_WhenCountOutOfRange(int count)
    {
        var validator = new RecentWorkOrdersQueryValidator();

        var result = validator.TestValidate(new RecentWorkOrdersQuery(count));

        result.ShouldHaveValidationErrorFor(q => q.Count);
    }
}
