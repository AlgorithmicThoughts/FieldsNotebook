using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

/// <summary>Maps dbo.UserProjects. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
public class UserProjectConfiguration : IEntityTypeConfiguration<UserProject>
{
    public void Configure(EntityTypeBuilder<UserProject> builder)
    {
        builder.ToTable("UserProjects", "dbo");

        builder.HasKey(e => e.Id).HasName("PK_UserProjects");
        builder.HasAlternateKey(e => new { e.UserId, e.ProjectId }).HasName("UQ_UserProjects_UserId_ProjectId");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // All relationships use ON DELETE NO ACTION, as in the SQL script.
        builder.HasOne<User>().WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_UserProjects_UserId");
        builder.HasOne<Project>().WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_UserProjects_ProjectId");
    }
}
