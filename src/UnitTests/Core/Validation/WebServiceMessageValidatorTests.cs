using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WebServiceMessageValidatorTests
{
    private readonly WebServiceMessageValidator _validator = new();

    [Test]
    public void ShouldPass_WhenTypeNameAndBodyAreSet()
    {
        var message = new WebServiceMessage { TypeName = "ClearMeasure.Bootcamp.Core.Model.Events.UserLoggedInEvent, ClearMeasure.Bootcamp.Core", Body = "{}" };
        var result = _validator.TestValidate(message);
        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenTypeNameIsEmpty()
    {
        var message = new WebServiceMessage { TypeName = "", Body = "{}" };
        var result = _validator.TestValidate(message);
        result.ShouldHaveValidationErrorFor(x => x.TypeName);
        result.ShouldNotHaveValidationErrorFor(x => x.Body);
    }

    [Test]
    public void ShouldFail_WhenTypeNameIsNull()
    {
        var message = new WebServiceMessage { TypeName = null!, Body = "{}" };
        var result = _validator.TestValidate(message);
        result.ShouldHaveValidationErrorFor(x => x.TypeName);
        result.ShouldNotHaveValidationErrorFor(x => x.Body);
    }

    [Test]
    public void ShouldFail_WhenTypeNameIsWhitespace()
    {
        var message = new WebServiceMessage { TypeName = "   ", Body = "{}" };
        var result = _validator.TestValidate(message);
        result.ShouldHaveValidationErrorFor(x => x.TypeName);
        result.ShouldNotHaveValidationErrorFor(x => x.Body);
    }

    [Test]
    public void ShouldFail_WhenBodyIsEmpty()
    {
        var message = new WebServiceMessage { TypeName = "ClearMeasure.Bootcamp.Core.Model.Events.UserLoggedInEvent, ClearMeasure.Bootcamp.Core", Body = "" };
        var result = _validator.TestValidate(message);
        result.ShouldHaveValidationErrorFor(x => x.Body);
        result.ShouldNotHaveValidationErrorFor(x => x.TypeName);
    }

    [Test]
    public void ShouldFail_WhenBodyIsNull()
    {
        var message = new WebServiceMessage { TypeName = "ClearMeasure.Bootcamp.Core.Model.Events.UserLoggedInEvent, ClearMeasure.Bootcamp.Core", Body = null! };
        var result = _validator.TestValidate(message);
        result.ShouldHaveValidationErrorFor(x => x.Body);
        result.ShouldNotHaveValidationErrorFor(x => x.TypeName);
    }

    [Test]
    public void ShouldFail_WhenBodyIsWhitespace()
    {
        var message = new WebServiceMessage { TypeName = "ClearMeasure.Bootcamp.Core.Model.Events.UserLoggedInEvent, ClearMeasure.Bootcamp.Core", Body = "   " };
        var result = _validator.TestValidate(message);
        result.ShouldHaveValidationErrorFor(x => x.Body);
        result.ShouldNotHaveValidationErrorFor(x => x.TypeName);
    }

    [Test]
    public void ShouldFail_WhenBothPropertiesBlank()
    {
        var message = new WebServiceMessage { TypeName = " ", Body = "" };
        var result = _validator.TestValidate(message);
        result.Errors.Count.ShouldBe(2);
        result.ShouldHaveValidationErrorFor(x => x.TypeName);
        result.ShouldHaveValidationErrorFor(x => x.Body);
    }
}
