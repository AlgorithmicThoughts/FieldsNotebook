using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations
{
    /// <summary>Maps dbo.UserSettings. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
    public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
    {
        public void Configure(EntityTypeBuilder<UserSettings> builder)
        {
            builder.ToTable("UserSettings", "dbo");

            builder.HasKey(e => e.UserId).HasName("PK_UserSettings");

            builder.Property(e => e.UserId).ValueGeneratedNever();
            builder.Property(e => e.ThemePreference).HasMaxLength(20).IsRequired();

            // All relationships use ON DELETE NO ACTION, as in the SQL script.
            builder.HasOne<User>().WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_UserSettings_UserId");
        }
    }
}
