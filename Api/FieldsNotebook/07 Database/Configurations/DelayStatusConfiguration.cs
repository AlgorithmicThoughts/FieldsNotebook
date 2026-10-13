using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

/// <summary>Maps dbo.DelayStatus. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
public class DelayStatusConfiguration : IEntityTypeConfiguration<DelayStatus>
{
    public void Configure(EntityTypeBuilder<DelayStatus> builder)
    {
        builder.ToTable("DelayStatus", "dbo");

        builder.HasKey(e => e.Id).HasName("PK_DelayStatus");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Code).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(e => e.Code, "UQ_DelayStatus_Code").IsUnique();
        builder.HasIndex(e => e.Name, "UQ_DelayStatus_Name").IsUnique();
    }
}
