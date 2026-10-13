using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

/// <summary>Maps dbo.Crew. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
public class CrewConfiguration : IEntityTypeConfiguration<Crew>
{
    public void Configure(EntityTypeBuilder<Crew> builder)
    {
        builder.ToTable("Crew", "dbo");

        builder.HasKey(e => e.Id).HasName("PK_Crew");
        builder.HasAlternateKey(e => new { e.Id, e.ProjectId }).HasName("UQ_Crew_Id_ProjectId");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();

        builder.HasIndex(e => e.ForemanLeadId, "UQ_Crew_ForemanLeadId").IsUnique();
        builder.HasIndex(e => new { e.ProjectId, e.Name }, "UQ_Crew_ProjectId_Name").IsUnique();

        // All relationships use ON DELETE NO ACTION, as in the SQL script.
        builder.HasOne<Project>().WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Crew_ProjectId");
        builder.HasOne<UserProject>().WithMany()
            .HasForeignKey(e => new { e.ForemanLeadId, e.ProjectId })
            .HasPrincipalKey(p => new { p.UserId, p.ProjectId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Crew_ForemanLeadId_ProjectId");
        builder.HasOne<UserProject>().WithMany()
            .HasForeignKey(e => new { e.SuperintendentUserId, e.ProjectId })
            .HasPrincipalKey(p => new { p.UserId, p.ProjectId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Crew_SuperintendentUserId_ProjectId");
    }
}
