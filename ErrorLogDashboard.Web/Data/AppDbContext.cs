using Microsoft.EntityFrameworkCore;
using ErrorLogDashboard.Web.Models;

namespace ErrorLogDashboard.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ErrorLog> ErrorLogs { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.User)
            .WithMany(u => u.Projects)
            .HasForeignKey(p => p.IdUser);

        modelBuilder.Entity<User>()
            .HasOne(u => u.SubscriptionPlan)
            .WithMany()
            .HasForeignKey(u => u.IdSubscriptionPlan);

        modelBuilder.Entity<ErrorLog>()
            .HasOne(e => e.Project)
            .WithMany(p => p.ErrorLogs)
            .HasForeignKey(e => e.IdProject);
    }
}
