using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.IntegrationTests.TestSupport;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpVendorToolTests
{
    [Test]
    public async Task ShouldListAllVendors()
    {
        var tag = TestData.Tag();
        var vendor1 = new Vendor
        {
            Name = $"HVAC Solutions-{tag}",
            Trade = "HVAC",
            ContactName = "Bob Smith",
            EmailAddress = "bob@hvac.com",
            PhoneNumber = "555-1234",
            IsActive = true
        };
        var vendor2 = new Vendor
        {
            Name = $"Plumbing Plus-{tag}",
            Trade = "Plumbing",
            ContactName = "Jane Doe",
            EmailAddress = "jane@plumbing.com",
            PhoneNumber = "555-5678",
            IsActive = true
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(vendor1);
        context.Add(vendor2);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await VendorTools.ListVendors(bus);

        result.ShouldContain(vendor1.Name);
        result.ShouldContain(vendor2.Name);
        result.ShouldContain("HVAC");
        result.ShouldContain("Plumbing");
        result.ShouldContain("Bob Smith");
        result.ShouldContain("Jane Doe");
    }

    [Test]
    public async Task ShouldReturnJsonArray()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await VendorTools.ListVendors(bus);

        using var json = JsonDocument.Parse(result);
        json.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Test]
    public async Task ShouldCreateVendor()
    {
        var vendorName = $"Electrical Works-{TestData.Tag()}";
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await VendorTools.CreateVendor(bus, vendorName, "Electrical", "Tom Jones", "tom@electrical.com", "555-9999");

        result.ShouldContain("\"succeeded\": true");
        result.ShouldContain(vendorName);

        // Verify the vendor was created
        var listResult = await VendorTools.ListVendors(bus);
        listResult.ShouldContain(vendorName);
        listResult.ShouldContain("Electrical");
        listResult.ShouldContain("Tom Jones");
    }

    [Test]
    public async Task ShouldCreateVendorWithDefaults()
    {
        var vendorName = $"Simple Vendor-{TestData.Tag()}";
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await VendorTools.CreateVendor(bus, vendorName, "General");

        result.ShouldContain("\"succeeded\": true");

        // Verify the vendor was created
        var listResult = await VendorTools.ListVendors(bus);
        listResult.ShouldContain(vendorName);
    }

    [Test]
    public async Task ShouldReturnErrorOnDuplicateVendorName()
    {
        var vendorName = $"HVAC Solutions-{TestData.Tag()}";
        var vendor = new Vendor { Name = vendorName, Trade = "HVAC", IsActive = true };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(vendor);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await VendorTools.CreateVendor(bus, vendorName, "HVAC Services");

        result.ShouldContain("already exists");
        result.ShouldNotContain("\"succeeded\": true");
    }

    [Test]
    public async Task ShouldReturnErrorOnDuplicateVendorNameIgnoringCase()
    {
        var vendorName = $"HVAC Solutions-{TestData.Tag()}";
        var vendor = new Vendor { Name = vendorName, Trade = "HVAC", IsActive = true };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(vendor);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await VendorTools.CreateVendor(bus, vendorName.ToLowerInvariant(), "HVAC Services");

        result.ShouldContain("already exists");
        result.ShouldNotContain("\"succeeded\": true");
    }
}
