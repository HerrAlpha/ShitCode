using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ErrorLogDashboard.Web.Data;
using ErrorLogDashboard.Web.Models;
using ErrorLogDashboard.Web.Services;

namespace ErrorLogDashboard.Web.Controllers;

[Authorize]
public class WebhookController : Controller
{
    private readonly AppDbContext _context;
    private readonly WebhookService _webhookService;

    public WebhookController(AppDbContext context, WebhookService webhookService)
    {
        _context = context;
        _webhookService = webhookService;
    }

    public async Task<IActionResult> Configure(Guid projectId)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var project = await _context.Projects
            .Include(p => p.Webhooks)
            .FirstOrDefaultAsync(p => p.IdProject == projectId && p.IdUser == userId);

        if (project == null)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(project);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid projectId, string name, string url, string? secretToken, WebhookType type = WebhookType.Generic)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.IdProject == projectId && p.IdUser == userId);

        if (project == null)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        // Validate URL format
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || 
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            TempData["Error"] = "Invalid webhook URL format";
            return RedirectToAction("Configure", new { projectId });
        }

        var webhook = new WebhookConfig
        {
            IdProject = projectId,
            Name = name,
            Url = url,
            SecretToken = secretToken,
            Type = type,
            IsActive = true
        };

        _context.WebhookConfigs.Add(webhook);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Webhook created successfully";
        return RedirectToAction("Configure", new { projectId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, Guid projectId)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var webhook = await _context.WebhookConfigs
            .Include(w => w.Project)
            .FirstOrDefaultAsync(w => w.IdWebhook == id);

        if (webhook != null && webhook.Project?.IdUser == userId)
        {
            _context.WebhookConfigs.Remove(webhook);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Webhook deleted successfully";
        }

        return RedirectToAction("Configure", new { projectId });
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(Guid id, Guid projectId)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var webhook = await _context.WebhookConfigs
            .Include(w => w.Project)
            .FirstOrDefaultAsync(w => w.IdWebhook == id);

        if (webhook != null && webhook.Project?.IdUser == userId)
        {
            webhook.IsActive = !webhook.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Webhook {(webhook.IsActive ? "enabled" : "disabled")}";
        }

        return RedirectToAction("Configure", new { projectId });
    }

    [HttpPost]
    public async Task<IActionResult> Test([FromBody] WebhookTestRequest request)
    {
        if (string.IsNullOrEmpty(request.Url))
        {
            return Json(new { success = false, message = "Please provide a valid webhook URL." });
        }

        var success = await _webhookService.TestWebhookAsync(request.Url, request.SecretToken);
        
        return Json(new { success, message = success 
            ? "Webhook test successful! Check your endpoint." 
            : "Webhook test failed. Please verify the URL." });
    }
}

public class WebhookTestRequest
{
    public string Url { get; set; } = string.Empty;
    public string? SecretToken { get; set; }
}

