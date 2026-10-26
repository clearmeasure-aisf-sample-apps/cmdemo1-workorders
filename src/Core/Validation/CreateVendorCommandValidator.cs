using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- Qodana C6 (#9039): discovered via FluentValidation's
// assembly-scan/DI reflection, not by direct reference; qodana.yaml exclude is documentation-only.
namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// Validator for <see cref="CreateVendorCommand"/>: the name is required, and every string fits its column.
/// Rules that depend on stored data are in <see cref="CreateVendorCommand.FindRuleViolations"/>.
/// </summary>
public sealed class CreateVendorCommandValidator : AbstractValidator<CreateVendorCommand>
{
    /// <summary>
    /// Defines the rules.
    /// </summary>
    public CreateVendorCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Vendor.NameMaxLength);
        RuleFor(x => x.Trade).MaximumLength(Vendor.TradeMaxLength);
        RuleFor(x => x.ContactName).MaximumLength(Vendor.ContactNameMaxLength);
        RuleFor(x => x.EmailAddress).MaximumLength(Vendor.EmailAddressMaxLength);
        RuleFor(x => x.EmailAddress)
            .EmailAddress()
            .WithMessage("Email address is not valid.")
            .When(x => !string.IsNullOrEmpty(x.EmailAddress));
        RuleFor(x => x.PhoneNumber).MaximumLength(Vendor.PhoneNumberMaxLength);
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^[0-9+\-.\(\) ]*$")
            .WithMessage("Phone number is not valid.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        RuleFor(x => x.PhoneNumber)
            .Must(ValidatePhoneDigitCount)
            .WithMessage("Phone number is not valid.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }

    private static bool ValidatePhoneDigitCount(string? phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
        {
            return true;
        }

        var digitCount = phoneNumber.Count(char.IsDigit);
        return digitCount >= 7;
    }
}
