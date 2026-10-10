using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations
{
    /// <summary>Maps dbo.Districts. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
    public class DistrictConfiguration : IEntityTypeConfiguration<District>
    {
        public void Configure(EntityTypeBuilder<District> builder)
        {
            builder.ToTable("Districts", "dbo");

            builder.HasKey(e => e.Id).HasName("PK_Districts");

            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
            builder.Property(e => e.Code).HasMaxLength(50).IsRequired();

            builder.HasIndex(e => e.Code, "UQ_Districts_Code").IsUnique();
        }
    }
}
