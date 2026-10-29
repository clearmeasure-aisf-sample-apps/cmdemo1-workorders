using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

/// <summary>
/// Maps <see cref="Team"/> to <c>dbo.Team</c> and its members to the join table <c>dbo.TeamMember</c>.
/// </summary>
public class TeamMap : IEntityFrameworkMapping
{
    /// <inheritdoc />
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(entity =>
        {
            entity.ToTable("Team", "dbo");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).IsRequired()
                .ValueGeneratedOnAdd()
                .HasDefaultValue(Guid.Empty);

            entity.Property(t => t.Name).IsRequired().HasMaxLength(Team.NameMaxLength);
            entity.Property(t => t.Description).HasMaxLength(Team.DescriptionMaxLength);
            entity.HasIndex(t => t.Name).IsUnique().HasDatabaseName("UX_Team_Name");
            entity.Ignore(t => t.MemberCount);

            entity.HasMany(t => t.Members)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "TeamMember",
                    r => r.HasOne<Employee>().WithMany().HasForeignKey("EmployeeId")
                        .OnDelete(DeleteBehavior.Restrict),
                    l => l.HasOne<Team>().WithMany().HasForeignKey("TeamId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("TeamId", "EmployeeId");
                        j.ToTable("TeamMember", "dbo");
                    });
        });
    }
}
