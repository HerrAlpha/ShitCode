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
        _ = Task.Run(async () =>
        {
            try
            {
                var summary = await _deepSeekService.GenerateErrorSummaryAsync(errorLog.Message, errorLog.StackTrace);
                if (!string.IsNullOrEmpty(summary))
                {
                    errorLog.Summary = summary;
                    await _context.SaveChangesAsync();
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
