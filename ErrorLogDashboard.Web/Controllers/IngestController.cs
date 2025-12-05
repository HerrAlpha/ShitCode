using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErrorLogDashboard.Web.Data;
using ErrorLogDashboard.Web.Models;

namespace ErrorLogDashboard.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IngestController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly Services.DeepSeekService _deepSeekService;

    public IngestController(AppDbContext context, Services.DeepSeekService deepSeekService)
    {
        _context = context;
        _deepSeekService = deepSeekService;
    }

    [HttpPost("{apiKey}")]
    public async Task<IActionResult> Post(string apiKey, [FromBody] ErrorLogDto dto, [FromHeader(Name = "X-Security-Key")] string securityKey)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.ApiKey == apiKey);
        if (project == null) return Unauthorized("Invalid API Key");
        
        if (project.SecurityKey != securityKey) return Unauthorized("Invalid Security Key");

        var errorLog = new ErrorLog
        {
            Message = dto.Message,
            StackTrace = dto.StackTrace,
            IdProject = project.IdProject,
            CreatedAt = DateTime.UtcNow
        };

        _context.ErrorLogs.Add(errorLog);
        await _context.SaveChangesAsync();

        // Generate AI summary asynchronously (fire and forget)
        // We need to capture the ID and use a new scope because the request context will be disposed
        var errorLogId = errorLog.IdErrorLog;
        var message = errorLog.Message;
        var stackTrace = errorLog.StackTrace;
        var serviceProvider = HttpContext.RequestServices;

        _ = Task.Run(async () =>
        {
            try
            {
                // Create a new scope to resolve scoped services like AppDbContext
                using (var scope = serviceProvider.CreateScope())
                {
                    var scopedContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var deepSeekService = scope.ServiceProvider.GetRequiredService<Services.DeepSeekService>();

                    var summary = await deepSeekService.GenerateErrorSummaryAsync(message, stackTrace);
                    
                    if (!string.IsNullOrEmpty(summary))
                    {
                        // Re-fetch the error log from the new context
                        var logToUpdate = await scopedContext.ErrorLogs.FindAsync(errorLogId);
                        if (logToUpdate != null)
                        {
                            logToUpdate.Summary = summary;
                            await scopedContext.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate summary: {ex.Message}");
            }
        });

        return Ok(new { success = true, id = errorLog.IdErrorLog });
    }
}

public class ErrorLogDto
{
    public string ApiKey { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
}
