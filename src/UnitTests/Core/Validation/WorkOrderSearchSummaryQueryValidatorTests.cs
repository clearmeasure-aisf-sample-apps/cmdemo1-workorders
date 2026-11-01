using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WorkOrderSearchSummaryQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenUserNameNullOrSet()
    {
        var validator = new WorkOrderSearchSummaryQueryValidator();

        validator.TestValidate(new WorkOrderSearchSummaryQuery(null)).IsValid.ShouldBeTrue();
        validator.TestValidate(new WorkOrderSearchSummaryQuery("tlovejoy")).IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenUserNameTooLong()
    {
        var validator = new WorkOrderSearchSummaryQueryValidator();

        validator.TestValidate(new WorkOrderSearchSummaryQuery(new string('u', 257)))
            .ShouldHaveValidationErrorFor(x => x.UserName);
    }

    [Test]
    public void ShouldPass_ForBuildingsQuery()
    {
        new WorkOrderBuildingsQueryValidator().TestValidate(new WorkOrderBuildingsQuery()).IsValid.ShouldBeTrue();
        new WorkOrderReportsQueryValidator().TestValidate(new WorkOrderReportsQuery()).IsValid.ShouldBeTrue();
    }
}
