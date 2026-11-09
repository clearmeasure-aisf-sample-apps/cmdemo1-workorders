using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class WorkOrderMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.ToTable("WorkOrder", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired()
                .ValueGeneratedOnAdd()
                .HasDefaultValue(Guid.Empty);
            entity.Property(e => e.Number).IsRequired().HasMaxLength(7);
            entity.HasIndex(e => e.Number).IsUnique().HasDatabaseName("IX_WorkOrder_Number");
            MapColumns(entity);
            MapEmployees(entity);
            entity.Property(e => e.Status)
                .HasConversion(new WorkOrderStatusConverter())
                .HasMaxLength(3);
        });
    }

    private static void MapColumns(EntityTypeBuilder<WorkOrder> entity)
    {
        entity.Property(e => e.Title).IsRequired().HasMaxLength(WorkOrder.TitleMaxLength);
        entity.Property(e => e.Description).HasMaxLength(4000);
        entity.Property(e => e.Instructions).HasMaxLength(WorkOrder.InstructionsMaxLength);
        entity.Property(e => e.RoomNumber).HasMaxLength(WorkOrder.RoomNumberMaxLength);
        entity.Property(e => e.DueDate).HasColumnType("date");
        entity.Property(e => e.Priority).HasConversion<byte>().IsRequired();
        entity.Property(e => e.Building).HasMaxLength(WorkOrder.BuildingMaxLength);
        entity.Property(e => e.Floor).HasMaxLength(WorkOrder.FloorMaxLength);
        entity.Property(e => e.ExternalReference).HasMaxLength(WorkOrder.ExternalReferenceMaxLength);
        entity.Property(e => e.AccessNotes).HasMaxLength(WorkOrder.AccessNotesMaxLength);
        entity.Property(e => e.WaitingOnVendorSince).IsRequired(false);
    }

    /// <summary>Creator and assignee, both loaded with every work order.</summary>
    private static void MapEmployees(EntityTypeBuilder<WorkOrder> entity)
    {
        entity.HasOne(e => e.Creator)
            .WithMany()
            .HasForeignKey("CreatorId")
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Assignee)
            .WithMany()
            .HasForeignKey("AssigneeId")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Navigation(e => e.Creator).AutoInclude();
        entity.Navigation(e => e.Assignee).AutoInclude();
    }
}
