using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class CreateVendorCommandValidatorTests
{
    private CreateVendorCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateVendorCommandValidator();
    }

    [Test]
    public void ShouldPass_WhenAllFieldsValid()
    {
        var command = new CreateVendorCommand("Acme", "HVAC", "Pat Smith", "pat@acme.test", "555-0100");

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenAllOptionalFieldsNull()
    {
        var command = new CreateVendorCommand("Acme", null, null, null, null, false);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void ShouldFail_WhenNameIsEmpty(string name)
    {
        var command = new CreateVendorCommand(name, null, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void ShouldFail_WhenNameIs101Chars()
    {
        var command = new CreateVendorCommand(new string('a', 101), null, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void ShouldPass_WhenNameIsExactly100Chars()
    {
        var command = new CreateVendorCommand(new string('a', 100), null, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void ShouldFail_WhenTradeIs101Chars()
    {
        var command = new CreateVendorCommand("Acme", new string('t', 101), null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Trade);
    }

    [Test]
    public void ShouldFail_WhenContactNameIs101Chars()
    {
        var command = new CreateVendorCommand("Acme", null, new string('c', 101), null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ContactName);
    }

    [Test]
    public void ShouldFail_WhenEmailAddressIs256Chars()
    {
        var command = new CreateVendorCommand("Acme", null, null, new string('e', 256), null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.EmailAddress);
    }

    [Test]
    public void ShouldFail_WhenPhoneNumberIs51Chars()
    {
        var command = new CreateVendorCommand("Acme", null, null, null, new string('5', 51));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Test]
    public void ShouldPass_WhenOptionalFieldsAreAtTheirMaximumLengths()
    {
        var longEmail = new string('a', 242) + "@test.com";
        var command = new CreateVendorCommand("Acme", new string('t', 100), new string('c', 100),
            longEmail, new string('5', 50));

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldReportDuplicate_WhenNameMatchesExistingIgnoringCase()
    {
        var command = new CreateVendorCommand("acme", null, null, null, null);

        var violations = command.FindRuleViolations(["Bolt", "Acme"]);

        violations.ShouldBe(["A vendor named 'acme' already exists."]);
    }

    [Test]
    public void ShouldReturnNoViolations_WhenNameIsUnique()
    {
        var command = new CreateVendorCommand("Zed", null, null, null, null);

        var violations = command.FindRuleViolations(["Bolt", "Acme"]);

        violations.ShouldBeEmpty();
    }

    [Test]
    public void ShouldPass_WhenEmailAddressIsValid()
    {
        var command = new CreateVendorCommand("Acme", null, null, "test@example.com", null);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenEmailAddressIsNull()
    {
        var command = new CreateVendorCommand("Acme", null, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.EmailAddress);
    }

    [Test]
    public void ShouldPass_WhenEmailAddressIsEmpty()
    {
        var command = new CreateVendorCommand("Acme", null, null, "", null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.EmailAddress);
    }

    [TestCase("not-an-email")]
    [TestCase("test@")]
    [TestCase("@example.com")]
    [TestCase("test")]
    public void ShouldFail_WhenEmailAddressIsInvalid(string email)
    {
        var command = new CreateVendorCommand("Acme", null, null, email, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.EmailAddress);
    }

    [Test]
    public void ShouldPass_WhenEmailAddressIsAtMaxLengthWithValidFormat()
    {
        var email = new string('a', 244) + "@test.com";
        var command = new CreateVendorCommand("Acme", null, null, email, null);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenPhoneNumberIsValid()
    {
        var command = new CreateVendorCommand("Acme", null, null, null, "555-0100");

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [TestCase("+1 (555) 012-0100")]
    [TestCase("5550100")]
    [TestCase("555 0100")]
    [TestCase("+15550100")]
    public void ShouldPass_WhenPhoneNumberIsValidFormat(string phone)
    {
        var command = new CreateVendorCommand("Acme", null, null, null, phone);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenPhoneNumberIsNull()
    {
        var command = new CreateVendorCommand("Acme", null, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Test]
    public void ShouldPass_WhenPhoneNumberIsEmpty()
    {
        var command = new CreateVendorCommand("Acme", null, null, null, "");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [TestCase("555-01")]
    [TestCase("123")]
    [TestCase("      ")]
    public void ShouldFail_WhenPhoneNumberHasFewerThan7Digits(string phone)
    {
        var command = new CreateVendorCommand("Acme", null, null, null, phone);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [TestCase("555@0100")]
    [TestCase("555#0100")]
    [TestCase("555*0100")]
    public void ShouldFail_WhenPhoneNumberContainsInvalidCharacters(string phone)
    {
        var command = new CreateVendorCommand("Acme", null, null, null, phone);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Test]
    public void ShouldPass_WhenPhoneNumberIsAtMaxLengthWithValidFormat()
    {
        var phone = "(555) 012-0100 (555) 555-5555 555";
        var command = new CreateVendorCommand("Acme", null, null, null, phone);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenEmailAndPhoneAreValidTogether()
    {
        var command = new CreateVendorCommand("Acme", "HVAC", "Pat Smith", "pat@acme.test", "555-0100");

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }
}
