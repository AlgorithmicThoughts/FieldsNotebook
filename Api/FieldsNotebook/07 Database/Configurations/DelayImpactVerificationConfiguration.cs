using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations
{
    /// <summary>Maps dbo.DelayImpactVerifications. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
    public class DelayImpactVerificationConfiguration : IEntityTypeConfiguration<DelayImpactVerification>
    {
        public void Configure(EntityTypeBuilder<DelayImpactVerification> builder)
        {
            builder.ToTable("DelayImpactVerifications", "dbo");

            builder.HasKey(e => new { e.DelayImpactId, e.DelayCycleId }).HasName("PK_DelayImpactVerifications");

            builder.Property(e => e.VerifiedAt).HasPrecision(3);
            builder.Property(e => e.VerificationNote).HasMaxLength(1000);

            // All relationships use ON DELETE NO ACTION, as in the SQL script.
            builder.HasOne<DelayImpact>().WithMany()
                .HasForeignKey(e => new { e.DelayImpactId, e.DelayId })
                .HasPrincipalKey(p => new { p.Id, p.DelayId })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DelayImpactVerifications_DelayImpactId_DelayId");
            builder.HasOne<DelayCycle>().WithMany()
                .HasForeignKey(e => new { e.DelayCycleId, e.DelayId })
                .HasPrincipalKey(p => new { p.DelayCycleId, p.DelayId })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DelayImpactVerifications_DelayCycleId_DelayId");
            builder.HasOne<User>().WithMany()
                .HasForeignKey(e => e.VerifiedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DelayImpactVerifications_VerifiedByUserId");
        }
    }
}
