using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

/// <summary>Maps dbo.DelayImpacts. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
public class DelayImpactConfiguration : IEntityTypeConfiguration<DelayImpact>
{
    public void Configure(EntityTypeBuilder<DelayImpact> builder)
    {
        builder.ToTable("DelayImpacts", "dbo");

        builder.HasKey(e => e.Id).HasName("PK_DelayImpacts");
        builder.HasAlternateKey(e => new { e.Id, e.DelayId }).HasName("UQ_DelayImpacts_Id_DelayId");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.HasIndex(e => new { e.DelayId, e.BlockedWorkItemId, e.BlockedCrewId }, "UX_DelayImpacts_DelayId_BlockedWorkItemId_BlockedCrewId").IsUnique().HasFilter("[BlockedWorkItemId] IS NOT NULL");
        builder.HasIndex(e => new { e.DelayId, e.BlockedCrewId }, "UX_DelayImpacts_DelayId_BlockedCrewId").IsUnique().HasFilter("[BlockedWorkItemId] IS NULL");

        // All relationships use ON DELETE NO ACTION, as in the SQL script.
        builder.HasOne<Project>().WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_DelayImpacts_ProjectId");
        builder.HasOne<Delay>().WithMany()
            .HasForeignKey(e => new { e.DelayId, e.ProjectId })
            .HasPrincipalKey(p => new { p.Id, p.ProjectId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_DelayImpacts_DelayId_ProjectId");
        builder.HasOne<Crew>().WithMany()
            .HasForeignKey(e => new { e.BlockedCrewId, e.ProjectId })
            .HasPrincipalKey(p => new { p.Id, p.ProjectId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_DelayImpacts_BlockedCrewId_ProjectId");
        builder.HasOne<WorkItem>().WithMany()
            .HasForeignKey(e => new { e.BlockedWorkItemId, e.ProjectId })
            .HasPrincipalKey(p => new { p.Id, p.ProjectId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_DelayImpacts_BlockedWorkItemId_ProjectId");
    }
}
