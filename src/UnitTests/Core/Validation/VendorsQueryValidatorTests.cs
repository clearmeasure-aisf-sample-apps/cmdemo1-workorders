using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class VendorsQueryValidatorTests
{
    [Test]
    public void ShouldPass_ForDefaultQuery()
    {
        var validator = new VendorsQueryValidator();

        var result = validator.TestValidate(new VendorsQuery());

        result.IsValid.ShouldBeTrue();
    }
}
