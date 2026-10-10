using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Database
{
    // Schema is owned by the SQL scripts; this context maps existing tables and never creates or migrates them.
    public class FieldNotebookContext(DbContextOptions<FieldNotebookContext> options) : DbContext(options)
    {
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<District> Districts => Set<District>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<UserProject> UserProjects => Set<UserProject>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Crew> Crew => Set<Crew>();
        public DbSet<Segment> Segments => Set<Segment>();
        public DbSet<WorkItem> WorkItems => Set<WorkItem>();
        public DbSet<WorkItemTeamAssignment> WorkItemTeamAssignment => Set<WorkItemTeamAssignment>();
        public DbSet<WorkItemDependency> WorkItemDependencies => Set<WorkItemDependency>();
        public DbSet<DelayTag> DelayTags => Set<DelayTag>();
        public DbSet<SiteMap> SiteMaps => Set<SiteMap>();
        public DbSet<DelayStatus> DelayStatus => Set<DelayStatus>();
        public DbSet<Delay> Delays => Set<Delay>();
        public DbSet<DelayImpact> DelayImpacts => Set<DelayImpact>();
        public DbSet<DelayCycle> DelayCycles => Set<DelayCycle>();
        public DbSet<DelayImpactVerification> DelayImpactVerifications => Set<DelayImpactVerification>();
        public DbSet<DelayPhoto> DelayPhotos => Set<DelayPhoto>();
        public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<UserSettings> UserSettings => Set<UserSettings>();

        // The SQL script creates no per-foreign-key indexes, so the model must not imply them.
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
            configurationBuilder.Conventions.Remove<ForeignKeyIndexConvention>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FieldNotebookContext).Assembly);
    }
}
