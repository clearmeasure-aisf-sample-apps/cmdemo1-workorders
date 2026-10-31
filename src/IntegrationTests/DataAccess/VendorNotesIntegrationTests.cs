using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

/// <summary>
/// Verifies persistence of optional vendor notes.
/// </summary>
[TestFixture]
public class VendorNotesIntegrationTests
{
    [TestCase(null)]
    [TestCase(0)]
    [TestCase(1000)]
    public async Task ShouldPersistNotes_WithOptionalLength_RoundTrips(int? length)
    {
        var notes = length.HasValue ? new string('é', length.Value) : null;
        var vendor = new Vendor
        {
            Name = "VendorNotes_" + Guid.NewGuid().ToString("N"),
            Notes = notes
        };
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(vendor);
            await context.SaveChangesAsync();
        }

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var reloaded = await context.Set<Vendor>().SingleAsync(v => v.Id == vendor.Id);
            reloaded.Notes.ShouldBe(notes);
            var property = context.Model.FindEntityType(typeof(Vendor))!.FindProperty(nameof(Vendor.Notes))!;
            property.IsNullable.ShouldBeTrue();
            property.GetMaxLength().ShouldBe(1000);
        }
    }
}
