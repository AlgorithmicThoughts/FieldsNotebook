using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations
{
    /// <summary>Maps dbo.WorkItemTeamAssignment. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
    public class WorkItemTeamAssignmentConfiguration : IEntityTypeConfiguration<WorkItemTeamAssignment>
    {
        public void Configure(EntityTypeBuilder<WorkItemTeamAssignment> builder)
        {
            builder.ToTable("WorkItemTeamAssignment", "dbo");

            builder.HasKey(e => e.Id).HasName("PK_WorkItemTeamAssignment");

            builder.Property(e => e.Id).ValueGeneratedOnAdd();

            builder.HasIndex(e => new { e.WorkItemId, e.CrewId }, "UQ_WorkItemTeamAssignment_WorkItemId_CrewId").IsUnique();

            // All relationships use ON DELETE NO ACTION, as in the SQL script.
            builder.HasOne<Project>().WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_WorkItemTeamAssignment_ProjectId");
            builder.HasOne<WorkItem>().WithMany()
                .HasForeignKey(e => new { e.WorkItemId, e.ProjectId })
                .HasPrincipalKey(p => new { p.Id, p.ProjectId })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_WorkItemTeamAssignment_WorkItemId_ProjectId");
            builder.HasOne<Crew>().WithMany()
                .HasForeignKey(e => new { e.CrewId, e.ProjectId })
                .HasPrincipalKey(p => new { p.Id, p.ProjectId })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_WorkItemTeamAssignment_CrewId_ProjectId");
        }
    }
}
