using ErrorLogDashboard.Web.Models;

namespace ErrorLogDashboard.Web.Data;

public static class DataSeeder
{
    public static void Seed(AppDbContext context)
    {
        if (context.Users.Any()) return;

        var freePlan = new SubscriptionPlan { IdSubscriptionPlan = Guid.NewGuid(), Name = "Free", Price = 0, Description = "For Hobbyists", MaxProjects = 1, MaxDailyLogs = 100 };
        var basicPlan = new SubscriptionPlan { IdSubscriptionPlan = Guid.NewGuid(), Name = "Basic", Price = 9.99m, Description = "For Freelancers", MaxProjects = 5, MaxDailyLogs = 1000 };
        var proPlan = new SubscriptionPlan { IdSubscriptionPlan = Guid.NewGuid(), Name = "Pro", Price = 29.99m, Description = "For Small Teams", MaxProjects = 20, MaxDailyLogs = 10000 };
        var platinumPlan = new SubscriptionPlan { IdSubscriptionPlan = Guid.NewGuid(), Name = "Platinum", Price = 99.99m, Description = "For Enterprises", MaxProjects = 9999, MaxDailyLogs = 999999 };

        context.SubscriptionPlans.AddRange(freePlan, basicPlan, proPlan, platinumPlan);
        context.SaveChanges();

        var admin = new User
        {
            IdUser = Guid.NewGuid(),
            Name = "Admin User",
            Email = "admin@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = "Admin",
            IdSubscriptionPlan = platinumPlan.IdSubscriptionPlan
        };

        var freeUser = new User { IdUser = Guid.NewGuid(), Name = "Free User", Email = "free@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("free123"), Role = "User", IdSubscriptionPlan = freePlan.IdSubscriptionPlan };
        var basicUser = new User { IdUser = Guid.NewGuid(), Name = "Basic User", Email = "basic@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("basic123"), Role = "User", IdSubscriptionPlan = basicPlan.IdSubscriptionPlan };
        var proUser = new User { IdUser = Guid.NewGuid(), Name = "Pro User", Email = "pro@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pro123"), Role = "User", IdSubscriptionPlan = proPlan.IdSubscriptionPlan };

        context.Users.AddRange(admin, freeUser, basicUser, proUser);
        context.SaveChanges();

        var project = new Project
        {
            IdProject = Guid.NewGuid(),
            Name = "Demo Project",
            TechStack = ".NET 8 MVC",
            ApiKey = "demo-api-key",
            SecurityKey = "demo-security-key-very-secure",
            IdUser = admin.IdUser
        };
        context.Projects.Add(project);
        context.SaveChanges();
        
        context.ErrorLogs.Add(new ErrorLog
        {
            IdErrorLog = Guid.NewGuid(),
            Message = "NullReferenceException in HomeController",
            StackTrace = "at HomeController.Index() line 20",
            IdProject = project.IdProject,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        });
        context.SaveChanges();
    }
}
