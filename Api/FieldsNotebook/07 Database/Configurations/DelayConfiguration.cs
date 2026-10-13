using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

/// <summary>Maps dbo.Delays. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
public class DelayConfiguration : IEntityTypeConfiguration<Delay>
{
    public void Configure(EntityTypeBuilder<Delay> builder)
    {
        builder.ToTable("Delays", "dbo");

        builder.HasKey(e => e.Id).HasName("PK_Delays");
        builder.HasAlternateKey(e => new { e.Id, e.ProjectId }).HasName("UQ_Delays_Id_ProjectId");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Number).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.ReportType).HasMaxLength(30).IsRequired();
        builder.Property(e => e.PinX).HasPrecision(9, 6);
        builder.Property(e => e.PinY).HasPrecision(9, 6);
        builder.Property(e => e.LocationSource).HasMaxLength(30).IsRequired();
        builder.Property(e => e.LocationName).HasMaxLength(200);
        builder.Property(e => e.CreatedAt).HasPrecision(3);

        builder.HasIndex(e => e.Number, "UQ_Delays_Number").IsUnique();

        // All relationships use ON DELETE NO ACTION, as in the SQL script.
        builder.HasOne<Project>().WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Delays_ProjectId");
        builder.HasOne<DelayTag>().WithMany()
            .HasForeignKey(e => e.DelayTagId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Delays_DelayTagId");
        builder.HasOne<DelayStatus>().WithMany()
            .HasForeignKey(e => e.DelayStatusId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Delays_DelayStatusId");
        builder.HasOne<UserProject>().WithMany()
            .HasForeignKey(e => new { e.ReportedByUserId, e.ProjectId })
            .HasPrincipalKey(p => new { p.UserId, p.ProjectId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Delays_ReportedByUserId_ProjectId");
        builder.HasOne<Crew>().WithMany()
            .HasForeignKey(e => new { e.ReportedByCrewId, e.ProjectId })
            .HasPrincipalKey(p => new { p.Id, p.ProjectId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Delays_ReportedByCrewId_ProjectId");
        builder.HasOne<Crew>().WithMany()
            .HasForeignKey(e => new { e.OwnerCrewId, e.ProjectId })
            .HasPrincipalKey(p => new { p.Id, p.ProjectId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Delays_OwnerCrewId_ProjectId");
        builder.HasOne<WorkItem>().WithMany()
            .HasForeignKey(e => new { e.BlockingWorkItemId, e.ProjectId })
            .HasPrincipalKey(p => new { p.Id, p.ProjectId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Delays_BlockingWorkItemId_ProjectId");
        builder.HasOne<SiteMap>().WithMany()
            .HasForeignKey(e => new { e.SiteMapId, e.ProjectId })
            .HasPrincipalKey(p => new { p.Id, p.ProjectId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Delays_SiteMapId_ProjectId");
        builder.HasOne<Segment>().WithMany()
            .HasForeignKey(e => new { e.SegmentId, e.ProjectId })
            .HasPrincipalKey(p => new { p.Id, p.ProjectId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Delays_SegmentId_ProjectId");
    }
}
