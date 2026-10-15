using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class VendorQueryHandlerTests
{
    [SetUp]
    public void SetUp()
    {
        new DatabaseTests().Clean();
    }

    [Test]
    public async Task ShouldReturnActiveVendorsFirstThenByName()
    {
        var handler = new VendorQueryHandler(TestHost.GetRequiredService<DataContext>());
        (await handler.Handle(new CreateVendorCommand("Acme", null, null, null, null, false))).Succeeded.ShouldBeTrue();
        (await handler.Handle(new CreateVendorCommand("Zed", null, null, null, null))).Succeeded.ShouldBeTrue();
        (await handler.Handle(new CreateVendorCommand("Bolt", null, null, null, null))).Succeeded.ShouldBeTrue();

        var vendors = await new VendorQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new VendorsQuery());

        vendors.Select(v => v.Name).ShouldBe(["Bolt", "Zed", "Acme"]);
        vendors.Single(v => v.Name == "Acme").IsActive.ShouldBeFalse();
        vendors.Select(v => v.Id).Distinct().Count().ShouldBe(3);
    }

    [Test]
    public async Task ShouldPersistAllVendorFields()
    {
        var result = await new VendorQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new CreateVendorCommand(" Acme Heating ", "HVAC", "Pat Smith", "pat@acme.test", "555-0100"));

        result.Succeeded.ShouldBeTrue(result.Message);
        result.Id.ShouldNotBeNull();
        var vendor = (await new VendorQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new VendorsQuery())).ShouldHaveSingleItem();
        vendor.Id.ShouldBe(result.Id.Value);
        vendor.Name.ShouldBe("Acme Heating");
        vendor.Trade.ShouldBe("HVAC");
        vendor.ContactName.ShouldBe("Pat Smith");
        vendor.EmailAddress.ShouldBe("pat@acme.test");
        vendor.PhoneNumber.ShouldBe("555-0100");
        vendor.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldFail_WhenVendorNameDuplicatesIgnoringCase()
    {
        (await new VendorQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new CreateVendorCommand("Acme", null, null, null, null))).Succeeded.ShouldBeTrue();

        var result = await new VendorQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new CreateVendorCommand("acme", "Plumbing", null, null, null));

        result.Succeeded.ShouldBeFalse();
        result.Id.ShouldBeNull();
        result.Message.ShouldContain("already exists");
        await using var context = TestHost.GetRequiredService<DataContext>();
        (await context.Set<Vendor>().CountAsync()).ShouldBe(1);
    }
}
