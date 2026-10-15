using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class SaveSearchViewCommandValidatorTests
{
    private readonly SaveSearchViewCommandValidator _validator = new();

    [Test]
    public void ShouldPass_WhenAllFieldsValid()
    {
        _validator.TestValidate(new SaveSearchViewCommand("hsimpson", "Mine", "Priority=High")).IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenNameIsEmpty()
    {
        _validator.TestValidate(new SaveSearchViewCommand("hsimpson", "", "")).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void ShouldFail_WhenNameIsOver100Characters()
    {
        _validator.TestValidate(new SaveSearchViewCommand("hsimpson", new string('a', 101), ""))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void ShouldFail_WhenQueryIsOver1000Characters()
    {
        _validator.TestValidate(new SaveSearchViewCommand("hsimpson", "Mine", new string('q', 1001)))
            .ShouldHaveValidationErrorFor(x => x.Query);
    }

    [Test]
    public void ShouldFail_WhenUserNameIsEmpty()
    {
        _validator.TestValidate(new SaveSearchViewCommand("", "Mine", "")).ShouldHaveValidationErrorFor(x => x.UserName);
    }
}
