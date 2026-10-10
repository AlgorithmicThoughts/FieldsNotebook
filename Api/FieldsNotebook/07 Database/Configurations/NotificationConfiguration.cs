using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations
{
    /// <summary>Maps dbo.Notifications. Generated from Field-Notebook-Create-And-Seed.sql; the SQL script owns the schema.</summary>
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications", "dbo");

            builder.HasKey(e => e.Id).HasName("PK_Notifications");

            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.CreatedAt).HasPrecision(3);
            builder.Property(e => e.ReadAt).HasPrecision(3);

            builder.HasIndex(e => new { e.ActivityEventId, e.UserId }, "UQ_Notifications_ActivityEventId_UserId").IsUnique();

            // All relationships use ON DELETE NO ACTION, as in the SQL script.
            builder.HasOne<UserProject>().WithMany()
                .HasForeignKey(e => new { e.UserId, e.ProjectId })
                .HasPrincipalKey(p => new { p.UserId, p.ProjectId })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Notifications_UserId_ProjectId");
            builder.HasOne<Delay>().WithMany()
                .HasForeignKey(e => new { e.DelayId, e.ProjectId })
                .HasPrincipalKey(p => new { p.Id, p.ProjectId })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Notifications_DelayId_ProjectId");
            builder.HasOne<ActivityEvent>().WithMany()
                .HasForeignKey(e => new { e.ActivityEventId, e.DelayId, e.ProjectId })
                .HasPrincipalKey(p => new { p.Id, p.DelayId, p.ProjectId })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Notifications_ActivityEventId_DelayId_ProjectId");
        }
    }
}
