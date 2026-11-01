using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class SavedSearchViewMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SavedSearchView>(entity =>
        {
            entity.ToTable("SavedSearchView", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired().ValueGeneratedNever();
            entity.Property(e => e.EmployeeId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(SavedSearchView.MaxNameLength);
            entity.Property(e => e.Query).IsRequired().HasMaxLength(SavedSearchView.MaxQueryLength);
            entity.Property(e => e.CreatedDate).IsRequired();

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.EmployeeId, e.Name }).IsUnique();
        });
    }
}
