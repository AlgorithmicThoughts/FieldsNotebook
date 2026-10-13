using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

/// <summary>Maps dbo.Roles. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "dbo");

        builder.HasKey(e => e.Id).HasName("PK_Roles");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Name).HasMaxLength(50).IsRequired();

        builder.HasIndex(e => e.Name, "UQ_Roles_Name").IsUnique();
    }
}
