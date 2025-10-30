using Microsoft.EntityFrameworkCore;
using MiniPM.Models;

namespace MiniPM
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions opt) : base(opt) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectTask> Tasks => Set<ProjectTask>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<Project>()
                .HasOne<User>().WithMany()
                .HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectTask>()
                .HasOne(p => p.Project).WithMany(p => p.Tasks)
                .HasForeignKey(pt => pt.ProjectId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
