using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations
{
    /// <summary>Maps dbo.DelayCycles. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
    public class DelayCycleConfiguration : IEntityTypeConfiguration<DelayCycle>
    {
        public void Configure(EntityTypeBuilder<DelayCycle> builder)
        {
            builder.ToTable("DelayCycles", "dbo");

            builder.HasKey(e => e.DelayCycleId).HasName("PK_DelayCycles");
            builder.HasAlternateKey(e => new { e.DelayCycleId, e.DelayId }).HasName("UQ_DelayCycles_DelayCycleId_DelayId");

            builder.Property(e => e.DelayCycleId).ValueGeneratedOnAdd();
            builder.Property(e => e.CreatedAt).HasPrecision(3);
            builder.Property(e => e.ModifiedAt).HasPrecision(3);
            builder.Property(e => e.OpenReason).HasMaxLength(1000);
            builder.Property(e => e.AcknowledgmentChoice).HasMaxLength(30);
            builder.Property(e => e.AcknowledgedAt).HasPrecision(3);
            builder.Property(e => e.AcknowledgmentNote).HasMaxLength(1000);
            builder.Property(e => e.ExpectedReadyAt).HasPrecision(3);
            builder.Property(e => e.ClosedAt).HasPrecision(3);

            builder.HasIndex(e => e.DelayId, "UX_DelayCycles_DelayId").IsUnique().HasFilter("[ClosedAt] IS NULL");
            builder.HasIndex(e => new { e.DelayId, e.CreatedAt, e.DelayCycleId }, "IX_DelayCycles_History");

            // All relationships use ON DELETE NO ACTION, as in the SQL script.
            builder.HasOne<Delay>().WithMany()
                .HasForeignKey(e => e.DelayId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DelayCycles_DelayId");
            builder.HasOne<User>().WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DelayCycles_CreatedBy");
            builder.HasOne<User>().WithMany()
                .HasForeignKey(e => e.ModifiedBy)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DelayCycles_ModifiedBy");
            builder.HasOne<User>().WithMany()
                .HasForeignKey(e => e.AcknowledgedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DelayCycles_AcknowledgedByUserId");
            builder.HasOne<User>().WithMany()
                .HasForeignKey(e => e.ClosedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DelayCycles_ClosedByUserId");
        }
    }
}
