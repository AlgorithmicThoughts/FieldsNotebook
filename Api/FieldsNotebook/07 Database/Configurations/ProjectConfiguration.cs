using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

/// <summary>Maps dbo.Projects. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects", "dbo");

        builder.HasKey(e => e.Id).HasName("PK_Projects");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();

        builder.HasIndex(e => e.Code, "UQ_Projects_Code").IsUnique();

        // All relationships use ON DELETE NO ACTION, as in the SQL script.
        builder.HasOne<District>().WithMany()
            .HasForeignKey(e => e.DistrictId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Projects_DistrictId");
    }
}
