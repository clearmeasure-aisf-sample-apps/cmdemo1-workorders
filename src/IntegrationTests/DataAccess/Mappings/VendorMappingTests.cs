using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

[TestFixture]
public class VendorMappingTests
{
    [Test]
    public void ShouldPersistVendorNotes()
    {
        var notes = "Call before scheduling maintenance.";
        var vendor = new Vendor
        {
            Name = $"vendor_{TestData.Tag()}",
            Notes = notes
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(vendor);
            context.SaveChanges();
        }

        Vendor rehydratedVendor;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedVendor = context.Set<Vendor>()
                .Single(v => v.Id == vendor.Id);
        }

        rehydratedVendor.Notes.ShouldBe(notes);
    }
}
