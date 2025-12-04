using System.Security.Claims;
using ErrorLogDashboard.Web.Data;
using ErrorLogDashboard.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErrorLogDashboard.Web.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var user = await _context.Users
            .Include(u => u.SubscriptionPlan)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || user.PasswordHash != password) // In real app, use hashing
        {
            ModelState.AddModelError("", "Invalid email or password");
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.IdUser.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("Plan", user.SubscriptionPlan?.Name ?? "Free")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties { IsPersistent = true };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string name, string email, string password)
    {
        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            ModelState.AddModelError("", "Email already in use");
            return View();
        }

        var freePlan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Name == "Free");
        if (freePlan == null) return View("Error");

        var user = new User
        {
            IdUser = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = password, // In real app, use hashing
            Role = "User",
            IdSubscriptionPlan = freePlan.IdSubscriptionPlan
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Auto login
        return await Login(email, password);
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
