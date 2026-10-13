using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

/// <summary>Maps dbo.DelayTags. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
public class DelayTagConfiguration : IEntityTypeConfiguration<DelayTag>
{
    public void Configure(EntityTypeBuilder<DelayTag> builder)
    {
        builder.ToTable("DelayTags", "dbo");

        builder.HasKey(e => e.Id).HasName("PK_DelayTags");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(e => e.Code, "UQ_DelayTags_Code").IsUnique();
        builder.HasIndex(e => e.Name, "UQ_DelayTags_Name").IsUnique();
    }
}
