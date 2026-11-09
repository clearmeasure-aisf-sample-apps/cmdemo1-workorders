using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class WorkOrderStatusChangeMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrderStatusChange>(entity =>
        {
            entity.ToTable("WorkOrderStatusChange", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired()
                .ValueGeneratedOnAdd()
                .HasDefaultValue(Guid.Empty);
            MapColumns(entity);

            entity.HasOne(e => e.ChangedBy)
                .WithMany()
                .HasForeignKey("ChangedById")
                .OnDelete(DeleteBehavior.Restrict);
            entity.Navigation(e => e.ChangedBy).AutoInclude();
        });
    }

    private static void MapColumns(EntityTypeBuilder<WorkOrderStatusChange> entity)
    {
        var statusConverter = new WorkOrderStatusConverter();
        entity.Property(e => e.WorkOrderId).IsRequired();
        entity.Property(e => e.FromStatus).HasConversion(statusConverter).HasMaxLength(3).IsRequired();
        entity.Property(e => e.ToStatus).HasConversion(statusConverter).HasMaxLength(3).IsRequired();
        entity.Property(e => e.ChangedById).IsRequired();
        entity.Property(e => e.ChangedDate).IsRequired();
        entity.Property(e => e.Reason).HasMaxLength(InProgressToAssignedCommand.ReasonMaxLength);
    }
}
