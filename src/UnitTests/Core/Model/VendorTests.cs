using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class VendorTests
{
    [Test]
    public void ShouldDefaultIsActiveToTrue()
    {
        var vendor = new Vendor();

        vendor.IsActive.ShouldBeTrue();
    }

    [Test]
    public void ShouldInitializeOptionalFieldsToNull()
    {
        var vendor = new Vendor();

        vendor.Id.ShouldBe(Guid.Empty);
        vendor.Name.ShouldBeNull();
        vendor.Trade.ShouldBeNull();
        vendor.ContactName.ShouldBeNull();
        vendor.EmailAddress.ShouldBeNull();
        vendor.PhoneNumber.ShouldBeNull();
    }

    [Test]
    public void ShouldExposeMaxLengthConstants()
    {
        Vendor.NameMaxLength.ShouldBe(100);
        Vendor.TradeMaxLength.ShouldBe(100);
        Vendor.ContactNameMaxLength.ShouldBe(100);
        Vendor.EmailAddressMaxLength.ShouldBe(255);
        Vendor.PhoneNumberMaxLength.ShouldBe(50);
    }

    [Test]
    public void HasContactEmail_WhenEmailAddressIsNull_ShouldReturnFalse()
    {
        var vendor = new Vendor { EmailAddress = null };

        vendor.HasContactEmail.ShouldBeFalse();
    }

    [Test]
    public void HasContactEmail_WhenEmailAddressIsEmpty_ShouldReturnFalse()
    {
        var vendor = new Vendor { EmailAddress = string.Empty };

        vendor.HasContactEmail.ShouldBeFalse();
    }

    [Test]
    public void HasContactEmail_WhenEmailAddressIsWhitespace_ShouldReturnFalse()
    {
        var vendor = new Vendor { EmailAddress = "   " };

        vendor.HasContactEmail.ShouldBeFalse();
    }

    [Test]
    public void HasContactEmail_WhenEmailAddressHasValue_ShouldReturnTrue()
    {
        var vendor = new Vendor { EmailAddress = "a@b.com" };

        vendor.HasContactEmail.ShouldBeTrue();
    }
}
