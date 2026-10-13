using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

/// <summary>Maps dbo.Segments. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
public class SegmentConfiguration : IEntityTypeConfiguration<Segment>
{
    public void Configure(EntityTypeBuilder<Segment> builder)
    {
        builder.ToTable("Segments", "dbo");

        builder.HasKey(e => e.Id).HasName("PK_Segments");
        builder.HasAlternateKey(e => new { e.Id, e.ProjectId }).HasName("UQ_Segments_Id_ProjectId");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();

        builder.HasIndex(e => new { e.ProjectId, e.Name }, "UQ_Segments_ProjectId_Name").IsUnique();

        // All relationships use ON DELETE NO ACTION, as in the SQL script.
        builder.HasOne<Project>().WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Segments_ProjectId");
    }
}
