using ErrorLogDashboard.Web.Data;
using ErrorLogDashboard.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErrorLogDashboard.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalProjects = await _context.Projects.CountAsync();
        var totalErrors = await _context.ErrorLogs.CountAsync();
        
        // Simple revenue calculation (sum of user plan prices)
        var revenue = await _context.Users
            .Include(u => u.SubscriptionPlan)
            .SumAsync(u => u.SubscriptionPlan!.Price);

        var viewModel = new AdminDashboardViewModel
        {
            TotalUsers = totalUsers,
            TotalProjects = totalProjects,
            TotalErrors = totalErrors,
            TotalRevenue = revenue
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Users()
    {
        var users = await _context.Users
            .Include(u => u.SubscriptionPlan)
            .Include(u => u.Projects)
            .ToListAsync();

        return View(users);
    }

    public async Task<IActionResult> Projects()
    {
        var projects = await _context.Projects
            .Include(p => p.User)
            .Include(p => p.ErrorLogs)
            .ToListAsync();

        return View(projects);
    }
}

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalProjects { get; set; }
    public int TotalErrors { get; set; }
    public decimal TotalRevenue { get; set; }
}
