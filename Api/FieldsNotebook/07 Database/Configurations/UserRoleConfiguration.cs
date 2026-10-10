using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations
{
    /// <summary>Maps dbo.UserRoles. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("UserRoles", "dbo");

            builder.HasKey(e => e.UserRoleId).HasName("PK_UserRoles");

            builder.Property(e => e.UserRoleId).ValueGeneratedOnAdd();

            builder.HasIndex(e => new { e.UserId, e.ProjectId }, "UQ_UserRoles_UserId_ProjectId").IsUnique();

            // All relationships use ON DELETE NO ACTION, as in the SQL script.
            builder.HasOne<User>().WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_UserRoles_UserId");
            builder.HasOne<Role>().WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_UserRoles_RoleId");
            builder.HasOne<Project>().WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_UserRoles_ProjectId");
            builder.HasOne<UserProject>().WithMany()
                .HasForeignKey(e => new { e.UserId, e.ProjectId })
                .HasPrincipalKey(p => new { p.UserId, p.ProjectId })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_UserRoles_UserId_ProjectId");
        }
    }
}
