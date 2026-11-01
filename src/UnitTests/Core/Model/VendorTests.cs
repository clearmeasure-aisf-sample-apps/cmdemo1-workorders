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
}
