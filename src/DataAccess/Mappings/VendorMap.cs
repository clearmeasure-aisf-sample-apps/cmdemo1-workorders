using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

/// <summary>
/// Maps <see cref="Vendor"/> to <c>dbo.Vendor</c>.
/// </summary>
public class VendorMap : IEntityFrameworkMapping
{
    /// <inheritdoc />
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.ToTable("Vendor", "dbo");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Id).IsRequired()
                .ValueGeneratedOnAdd()
                .HasDefaultValue(Guid.Empty);

            entity.Property(v => v.Name).IsRequired().HasMaxLength(Vendor.NameMaxLength);
            entity.Property(v => v.Trade).HasMaxLength(Vendor.TradeMaxLength);
            entity.Property(v => v.ContactName).HasMaxLength(Vendor.ContactNameMaxLength);
            entity.Property(v => v.EmailAddress).HasMaxLength(Vendor.EmailAddressMaxLength);
            entity.Property(v => v.PhoneNumber).HasMaxLength(Vendor.PhoneNumberMaxLength);
            entity.Property(v => v.Notes).HasMaxLength(Vendor.NotesMaxLength);
            // No HasDefaultValue(true): EF would then omit IsActive = false from the INSERT and the row would
            // be saved as active. The DEFAULT 1 lives only in the SQL script, for raw inserts.
            entity.Property(v => v.IsActive).IsRequired();
            entity.HasIndex(v => v.Name).IsUnique().HasDatabaseName("UX_Vendor_Name");
        });
    }
}
