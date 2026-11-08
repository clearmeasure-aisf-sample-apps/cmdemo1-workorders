using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Server.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class EmployeeGetAllQueryValidatorTests
{
    [Test]
    public void ShouldPass_ForTheDefaultPage()
    {
        var validator = new EmployeeGetAllQueryValidator();
        var query = new EmployeeGetAllQuery();

        query.Skip.ShouldBe(0);
        query.Take.ShouldBe(EmployeeGetAllQuery.MaxTake);
        validator.TestValidate(query).IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenSkipIsNegative()
    {
        var validator = new EmployeeGetAllQueryValidator();

        var result = validator.TestValidate(new EmployeeGetAllQuery { Skip = -1, Take = 10 });

        result.ShouldHaveValidationErrorFor(query => query.Skip);
    }

    [Test]
    public void ShouldFail_WhenTakeIsOutsideTheCap()
    {
        var validator = new EmployeeGetAllQueryValidator();

        validator.TestValidate(new EmployeeGetAllQuery { Take = 0 })
            .ShouldHaveValidationErrorFor(query => query.Take);
        validator.TestValidate(new EmployeeGetAllQuery { Take = EmployeeGetAllQuery.MaxTake + 1 })
            .ShouldHaveValidationErrorFor(query => query.Take);
    }
}
