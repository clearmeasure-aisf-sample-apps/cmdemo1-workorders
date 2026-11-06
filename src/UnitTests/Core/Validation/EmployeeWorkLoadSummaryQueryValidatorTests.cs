using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class EmployeeWorkLoadSummaryQueryValidatorTests
{
    [Test]
    public void ShouldPass_ForEmployeeWorkLoadSummaryQuery()
    {
        new EmployeeWorkLoadSummaryQueryValidator().TestValidate(new EmployeeWorkLoadSummaryQuery()).IsValid.ShouldBeTrue();
    }
}
