using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

/// <summary>Maps dbo.ActivityEvents. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
public class ActivityEventConfiguration : IEntityTypeConfiguration<ActivityEvent>
{
    public void Configure(EntityTypeBuilder<ActivityEvent> builder)
    {
        builder.ToTable("ActivityEvents", "dbo");

        builder.HasKey(e => e.Id).HasName("PK_ActivityEvents");
        builder.HasAlternateKey(e => new { e.Id, e.DelayId, e.ProjectId }).HasName("UQ_ActivityEvents_Id_DelayId_ProjectId");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.EventType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.OldExpectedReadyAt).HasPrecision(3);
        builder.Property(e => e.NewExpectedReadyAt).HasPrecision(3);
        builder.Property(e => e.CreatedAt).HasPrecision(3);

        // All relationships use ON DELETE NO ACTION, as in the SQL script.
        builder.HasOne<Project>().WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ActivityEvents_ProjectId");
        builder.HasOne<DelayStatus>().WithMany()
            .HasForeignKey(e => e.FromDelayStatusId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ActivityEvents_FromDelayStatusId");
        builder.HasOne<DelayStatus>().WithMany()
            .HasForeignKey(e => e.ToDelayStatusId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ActivityEvents_ToDelayStatusId");
        builder.HasOne<Delay>().WithMany()
            .HasForeignKey(e => new { e.DelayId, e.ProjectId })
            .HasPrincipalKey(p => new { p.Id, p.ProjectId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ActivityEvents_DelayId_ProjectId");
        builder.HasOne<DelayCycle>().WithMany()
            .HasForeignKey(e => new { e.DelayCycleId, e.DelayId })
            .HasPrincipalKey(p => new { p.DelayCycleId, p.DelayId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ActivityEvents_DelayCycleId_DelayId");
        builder.HasOne<UserProject>().WithMany()
            .HasForeignKey(e => new { e.ActorUserId, e.ProjectId })
            .HasPrincipalKey(p => new { p.UserId, p.ProjectId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ActivityEvents_ActorUserId_ProjectId");
        builder.HasOne<DelayImpact>().WithMany()
            .HasForeignKey(e => new { e.DelayImpactId, e.DelayId })
            .HasPrincipalKey(p => new { p.Id, p.DelayId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ActivityEvents_DelayImpactId_DelayId");
    }
}
