using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

/// <summary>Maps dbo.SiteMaps. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
public class SiteMapConfiguration : IEntityTypeConfiguration<SiteMap>
{
    public void Configure(EntityTypeBuilder<SiteMap> builder)
    {
        builder.ToTable("SiteMaps", "dbo");

        builder.HasKey(e => e.Id).HasName("PK_SiteMaps");
        builder.HasAlternateKey(e => new { e.Id, e.ProjectId }).HasName("UQ_SiteMaps_Id_ProjectId");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Uri).HasMaxLength(2048).IsRequired();

        builder.HasIndex(e => new { e.ProjectId, e.Name }, "UQ_SiteMaps_ProjectId_Name").IsUnique();
        builder.HasIndex(e => e.ProjectId, "UX_SiteMaps_ProjectId").IsUnique().HasFilter("[IsActive] = 1");

        // All relationships use ON DELETE NO ACTION, as in the SQL script.
        builder.HasOne<Project>().WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_SiteMaps_ProjectId");
    }
}
