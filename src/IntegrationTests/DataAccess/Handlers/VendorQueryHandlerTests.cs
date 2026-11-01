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
public class VendorQueryHandlerTests
{
    private string _tag = "";

    [SetUp]
    public void SetUp()
    {
        _tag = TestData.Tag();
    }

    [Test]
    public async Task ShouldReturnActiveVendorsFirstThenByName()
    {
        var handler = new VendorQueryHandler(TestHost.GetRequiredService<DataContext>());
        (await handler.Handle(new CreateVendorCommand(VendorName("Acme"), null, null, null, null, false)))
            .Succeeded.ShouldBeTrue();
        (await handler.Handle(new CreateVendorCommand(VendorName("Zed"), null, null, null, null)))
            .Succeeded.ShouldBeTrue();
        (await handler.Handle(new CreateVendorCommand(VendorName("Bolt"), null, null, null, null)))
            .Succeeded.ShouldBeTrue();

        var vendors = await OwnVendors();

        vendors.Select(v => v.Name).ShouldBe([VendorName("Bolt"), VendorName("Zed"), VendorName("Acme")]);
        vendors.Single(v => v.Name == VendorName("Acme")).IsActive.ShouldBeFalse();
        vendors.Select(v => v.Id).Distinct().Count().ShouldBe(3);
    }

    [Test]
    public async Task ShouldPersistAllVendorFields()
    {
        var name = VendorName("Acme Heating");
        var result = await new VendorQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new CreateVendorCommand($" {name} ", "HVAC", "Pat Smith", "pat@acme.test", "555-0100"));

        result.Succeeded.ShouldBeTrue(result.Message);
        result.Id.ShouldNotBeNull();
        var vendor = (await OwnVendors()).ShouldHaveSingleItem();
        vendor.Id.ShouldBe(result.Id.Value);
        vendor.Name.ShouldBe(name);
        vendor.Trade.ShouldBe("HVAC");
        vendor.ContactName.ShouldBe("Pat Smith");
        vendor.EmailAddress.ShouldBe("pat@acme.test");
        vendor.PhoneNumber.ShouldBe("555-0100");
        vendor.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldFail_WhenVendorNameDuplicatesIgnoringCase()
    {
        var name = VendorName("Acme");
        (await new VendorQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new CreateVendorCommand(name, null, null, null, null))).Succeeded.ShouldBeTrue();

        var result = await new VendorQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new CreateVendorCommand(name.ToLowerInvariant(), "Plumbing", null, null, null));

        result.Succeeded.ShouldBeFalse();
        result.Id.ShouldBeNull();
        result.Message.ShouldContain("already exists");
        (await OwnVendors()).Length.ShouldBe(1);
    }

    private string VendorName(string name) => $"{name} {_tag}";

    private async Task<Vendor[]> OwnVendors()
    {
        var vendors = await new VendorQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new VendorsQuery());
        return vendors.Where(v => v.Name.EndsWith(_tag, StringComparison.Ordinal)).ToArray();
    }
}
