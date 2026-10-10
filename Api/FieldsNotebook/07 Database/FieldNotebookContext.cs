using Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Database
{
    // Schema is owned by the SQL scripts; this context maps existing tables and never creates or migrates them.
    public class FieldNotebookContext(DbContextOptions<FieldNotebookContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.DisplayName).HasMaxLength(150).IsRequired();
                entity.Property(u => u.Email).HasMaxLength(254).IsRequired();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Phone).HasMaxLength(30);
                entity.Property(u => u.IsActive).IsRequired();
            });
        }
    }
}
