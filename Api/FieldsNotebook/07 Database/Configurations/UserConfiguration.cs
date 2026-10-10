using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations
{
    /// <summary>Maps dbo.Users. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users", "dbo");

            builder.HasKey(e => e.Id).HasName("PK_Users");

            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.DisplayName).HasMaxLength(150).IsRequired();
            builder.Property(e => e.Email).HasMaxLength(254).IsRequired();
            builder.Property(e => e.Phone).HasMaxLength(30);

            builder.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();
        }
    }
}
