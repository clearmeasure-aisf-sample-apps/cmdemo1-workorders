using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.IntegrationTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

/// <summary>
/// The database is shared with other tests, so each test uses its own tag in vendor names and asserts
/// only on those rows.
/// </summary>
[TestFixture]
public class CreateVendorCommandIntegrationTests
{
    private string _tag = "";

    [SetUp]
    public void SetUp()
    {
        _tag = TestData.Tag();
    }

    [Test]
    public async Task ShouldCreateAndReadVendorWithValidEmailAndPhone()
    {
        var handler = new VendorQueryHandler(TestHost.GetRequiredService<DataContext>());
        var name = VendorName("Acme HVAC");
        var email = "pat@acme.test";
        var phone = "555-0100";

        var createResult = await handler.Handle(
            new CreateVendorCommand(name, "HVAC", "Pat Smith", email, phone));

        createResult.Succeeded.ShouldBeTrue(createResult.Message);
        createResult.Id.ShouldNotBeNull();

        var vendors = await handler.Handle(new VendorsQuery());
        var vendor = vendors.Single(v => v.Name == name);

        vendor.Id.ShouldBe(createResult.Id.Value);
        vendor.Name.ShouldBe(name);
        vendor.Trade.ShouldBe("HVAC");
        vendor.ContactName.ShouldBe("Pat Smith");
        vendor.EmailAddress.ShouldBe(email);
        vendor.PhoneNumber.ShouldBe(phone);
        vendor.IsActive.ShouldBeTrue();
    }

    private string VendorName(string name) => $"{name} {_tag}";
}
