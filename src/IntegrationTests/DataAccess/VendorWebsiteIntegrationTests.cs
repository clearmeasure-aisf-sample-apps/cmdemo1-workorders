using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

/// <summary>
/// Verifies persistence of optional vendor websites.
/// </summary>
[TestFixture]
public class VendorWebsiteIntegrationTests
{
    [TestCase(null)]
    [TestCase(0)]
    [TestCase(200)]
    public async Task ShouldPersistWebsite_WithOptionalLength_RoundTrips(int? length)
    {
        var website = length.HasValue ? new string('é', length.Value) : null;
        var vendor = new Vendor
        {
            Name = "VendorWebsite_" + Guid.NewGuid().ToString("N"),
            Website = website
        };
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(vendor);
            await context.SaveChangesAsync();
        }

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var reloaded = await context.Set<Vendor>().SingleAsync(v => v.Id == vendor.Id);
            reloaded.Website.ShouldBe(website);
            var property = context.Model.FindEntityType(typeof(Vendor))!.FindProperty(nameof(Vendor.Website))!;
            property.IsNullable.ShouldBeTrue();
            property.GetMaxLength().ShouldBe(200);
        }
    }
}
