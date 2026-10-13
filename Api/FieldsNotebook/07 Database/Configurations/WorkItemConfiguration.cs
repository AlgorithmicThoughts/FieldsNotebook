using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

/// <summary>Maps dbo.WorkItems. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
public class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.ToTable("WorkItems", "dbo");

        builder.HasKey(e => e.Id).HasName("PK_WorkItems");
        builder.HasAlternateKey(e => new { e.Id, e.ProjectId }).HasName("UQ_WorkItems_Id_ProjectId");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.PlannedStartAt).HasPrecision(3);
        builder.Property(e => e.PlannedFinishAt).HasPrecision(3);
        builder.Property(e => e.ExpectedFinishAt).HasPrecision(3);

        // All relationships use ON DELETE NO ACTION, as in the SQL script.
        builder.HasOne<Project>().WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_WorkItems_ProjectId");
        builder.HasOne<Segment>().WithMany()
            .HasForeignKey(e => new { e.SegmentId, e.ProjectId })
            .HasPrincipalKey(p => new { p.Id, p.ProjectId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_WorkItems_SegmentId_ProjectId");
    }
}
