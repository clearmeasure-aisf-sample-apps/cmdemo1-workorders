using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class TeamsQueryValidatorTests
{
    [Test]
    public void ShouldPass_ForDefaultQuery()
    {
        var validator = new TeamsQueryValidator();

        var result = validator.TestValidate(new TeamsQuery());

        result.IsValid.ShouldBeTrue();
    }
}
