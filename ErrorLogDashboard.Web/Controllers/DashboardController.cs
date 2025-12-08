using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErrorLogDashboard.Web.Data;
using ErrorLogDashboard.Web.Models;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ErrorLogDashboard.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var user = await _context.Users
            .Include(u => u.SubscriptionPlan)
            .Include(u => u.Projects)
            .ThenInclude(p => p.ErrorLogs)
            .FirstOrDefaultAsync(u => u.IdUser == userId);

        if (user == null) return RedirectToAction("Login", "Auth");

        var projects = user.Projects;
        
        var projectSummaries = projects.Select(p => 
        {
            var openCount = p.ErrorLogs.Count(e => e.Status == ErrorStatus.Open);
            var resolvedCount = p.ErrorLogs.Count(e => e.Status == ErrorStatus.Resolved);
            var totalCount = p.ErrorLogs.Count;
            var resolvePercentage = totalCount > 0 ? (resolvedCount * 100.0 / totalCount) : 0;
            
            return new ProjectSummaryViewModel
            {
                Id = p.IdProject,
                Name = p.Name,
                TechStack = p.TechStack,
                TotalErrors = totalCount,
                ErrorsLast24Hours = p.ErrorLogs.Count(e => e.CreatedAt >= DateTime.UtcNow.AddHours(-24)),
                OpenErrors = openCount,
                ResolvedErrors = resolvedCount,
                ResolvePercentage = resolvePercentage,
                ApiKey = p.ApiKey,
                SecurityKey = p.SecurityKey
            };
        }).ToList();
        
        var totalOpen = projectSummaries.Sum(p => p.OpenErrors);
        var totalResolved = projectSummaries.Sum(p => p.ResolvedErrors);
        var totalErrors = totalOpen + totalResolved;
        var overallResolvePercentage = totalErrors > 0 ? (totalResolved * 100.0 / totalErrors) : 0;
            
        var viewModel = new DashboardViewModel
        {
            SubscriptionPlan = user.SubscriptionPlan!,
            ProjectCount = projects.Count,
            Projects = projectSummaries,
            TotalOpenErrors = totalOpen,
            TotalResolvedErrors = totalResolved,
            OverallResolvePercentage = overallResolvePercentage
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> CreateProject()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var user = await _context.Users
            .Include(u => u.SubscriptionPlan)
            .Include(u => u.Projects)
            .FirstOrDefaultAsync(u => u.IdUser == userId);

        if (user == null) return RedirectToAction("Login", "Auth");

        // Check if user has reached project limit
        if (user.Projects.Count >= user.SubscriptionPlan?.MaxProjects)
        {
            TempData["Error"] = "You have reached your project limit. Please upgrade your plan.";
            return RedirectToAction("Index");
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject(string name, string techStack)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(techStack)) return RedirectToAction("Index");

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var user = await _context.Users
            .Include(u => u.SubscriptionPlan)
            .Include(u => u.Projects)
            .FirstOrDefaultAsync(u => u.IdUser == userId); 
        
        if (user == null || user.Projects.Count >= user.SubscriptionPlan?.MaxProjects)
        {
            // TODO: Show error message
            return RedirectToAction("Index");
        }
        
        var project = new Project
        {
            IdProject = Guid.NewGuid(),
            Name = name,
            TechStack = techStack,
            ApiKey = GenerateApiKey(),
            SecurityKey = GenerateSecurityKey(),
            IdUser = user.IdUser
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return RedirectToAction("ProjectCreated", new { id = project.IdProject });
    }

    public async Task<IActionResult> ProjectCreated(Guid id)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.IdProject == id && p.IdUser == userId);

        if (project == null)
        {
            return RedirectToAction("Index");
        }

        return View(project);
    }

    public async Task<IActionResult> ViewLogs(Guid id)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var project = await _context.Projects
            .Include(p => p.ErrorLogs.OrderByDescending(e => e.CreatedAt))
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.IdProject == id);

        // Check if project exists and user owns it
        if (project == null || project.IdUser != userId)
        {
            return RedirectToAction("Index");
        }

        return View(project);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.IdProject == id && p.IdUser == userId);

        if (project != null)
        {
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var errorLog = await _context.ErrorLogs
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.IdErrorLog == id);

        if (errorLog == null || errorLog.Project?.IdUser != userId)
        {
            return RedirectToAction("Index");
        }

        errorLog.Status = errorLog.Status == ErrorStatus.Open ? ErrorStatus.Resolved : ErrorStatus.Open;
        await _context.SaveChangesAsync();

        return RedirectToAction("ViewLogs", new { id = errorLog.IdProject });
    }

    private string GenerateApiKey()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLower();
    }

    private string GenerateSecurityKey()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower();
    }
}

public class DashboardViewModel
{
    public SubscriptionPlan SubscriptionPlan { get; set; } = new();
    public int ProjectCount { get; set; }
    public List<ProjectSummaryViewModel> Projects { get; set; } = new();
    public int TotalOpenErrors { get; set; }
    public int TotalResolvedErrors { get; set; }
    public double OverallResolvePercentage { get; set; }
}

public class ProjectSummaryViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TechStack { get; set; } = string.Empty;
    public int TotalErrors { get; set; }
    public int ErrorsLast24Hours { get; set; }
    public int OpenErrors { get; set; }
    public int ResolvedErrors { get; set; }
    public double ResolvePercentage { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string SecurityKey { get; set; } = string.Empty;
}
