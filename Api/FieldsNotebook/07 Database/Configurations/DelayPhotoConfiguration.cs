using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

/// <summary>Maps dbo.DelayPhotos. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
public class DelayPhotoConfiguration : IEntityTypeConfiguration<DelayPhoto>
{
    public void Configure(EntityTypeBuilder<DelayPhoto> builder)
    {
        builder.ToTable("DelayPhotos", "dbo");

        builder.HasKey(e => e.Id).HasName("PK_DelayPhotos");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Purpose).HasMaxLength(30).IsRequired();
        builder.Property(e => e.OriginalUrl).HasMaxLength(2048).IsRequired();
        builder.Property(e => e.ThumbnailUrl).HasMaxLength(2048).IsRequired();
        builder.Property(e => e.FileName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.CreatedAt).HasPrecision(3);
        builder.Property(e => e.UpdatedAt).HasPrecision(3);
        builder.Property(e => e.Latitude).HasPrecision(9, 6);
        builder.Property(e => e.Longitude).HasPrecision(9, 6);

        // All relationships use ON DELETE NO ACTION, as in the SQL script.
        builder.HasOne<User>().WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_DelayPhotos_CreatedByUserId");
        builder.HasOne<User>().WithMany()
            .HasForeignKey(e => e.UpdatedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_DelayPhotos_UpdatedByUserId");
        builder.HasOne<DelayCycle>().WithMany()
            .HasForeignKey(e => new { e.DelayCycleId, e.DelayId })
            .HasPrincipalKey(p => new { p.DelayCycleId, p.DelayId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_DelayPhotos_DelayCycleId_DelayId");
    }
}
