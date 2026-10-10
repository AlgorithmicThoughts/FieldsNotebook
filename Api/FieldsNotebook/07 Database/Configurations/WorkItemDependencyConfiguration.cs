using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations
{
    /// <summary>Maps dbo.WorkItemDependencies. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
    public class WorkItemDependencyConfiguration : IEntityTypeConfiguration<WorkItemDependency>
    {
        public void Configure(EntityTypeBuilder<WorkItemDependency> builder)
        {
            builder.ToTable("WorkItemDependencies", "dbo");

            builder.HasKey(e => e.Id).HasName("PK_WorkItemDependencies");

            builder.Property(e => e.Id).ValueGeneratedOnAdd();

            builder.HasIndex(e => new { e.PredecessorWorkItemId, e.SuccessorWorkItemId }, "UQ_WorkItemDependencies_PredecessorWorkItemId_SuccessorWorkItemId").IsUnique();

            // All relationships use ON DELETE NO ACTION, as in the SQL script.
            builder.HasOne<Project>().WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_WorkItemDependencies_ProjectId");
            builder.HasOne<WorkItem>().WithMany()
                .HasForeignKey(e => new { e.PredecessorWorkItemId, e.ProjectId })
                .HasPrincipalKey(p => new { p.Id, p.ProjectId })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_WorkItemDependencies_PredecessorWorkItemId_ProjectId");
            builder.HasOne<WorkItem>().WithMany()
                .HasForeignKey(e => new { e.SuccessorWorkItemId, e.ProjectId })
                .HasPrincipalKey(p => new { p.Id, p.ProjectId })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_WorkItemDependencies_SuccessorWorkItemId_ProjectId");
        }
    }
}
