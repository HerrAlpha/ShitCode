using System.Text;
using System.Text.Json;
using ErrorLogDashboard.Web.Data;
using ErrorLogDashboard.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ErrorLogDashboard.Web.Services;

public class WebhookService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IHttpClientFactory httpClientFactory, 
        IServiceScopeFactory serviceScopeFactory,
        ILogger<WebhookService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task TriggerWebhooksAsync(Guid projectId, ErrorLog errorLog)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var webhooks = await context.Set<WebhookConfig>()
                .Where(w => w.IdProject == projectId && w.IsActive)
                .ToListAsync();

            if (!webhooks.Any())
            {
                return;
            }

            var payload = new
            {
                @event = "error.created",
                timestamp = DateTime.UtcNow,
                error = new
                {
                    id = errorLog.IdErrorLog,
                    message = errorLog.Message,
                    stackTrace = errorLog.StackTrace,
                    summary = errorLog.Summary,
                    status = errorLog.Status.ToString(),
                    createdAt = errorLog.CreatedAt
                },
                project = new
                {
                    id = projectId
                }
            };

            var tasks = webhooks.Select(webhook => SendWebhookAsync(webhook, payload));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger webhooks for project {ProjectId}", projectId);
        }
    }

    private async Task SendWebhookAsync(WebhookConfig webhook, object payload)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            object formattedPayload;
            
            // Format message based on webhook type
            switch (webhook.Type)
            {
                case WebhookType.Discord:
                    formattedPayload = FormatDiscordMessage(payload);
                    break;
                case WebhookType.Telegram:
                    formattedPayload = FormatTelegramMessage(payload);
                    break;
                default:
                    formattedPayload = payload;
                    break;
            }

            var json = JsonSerializer.Serialize(formattedPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add secret token if configured (not used for Discord/Telegram)
            if (!string.IsNullOrEmpty(webhook.SecretToken) && webhook.Type == WebhookType.Generic)
            {
                client.DefaultRequestHeaders.Add("X-Webhook-Secret", webhook.SecretToken);
            }

            var response = await client.PostAsync(webhook.Url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Webhook {WebhookId} returned status {StatusCode} for URL {Url}", 
                    webhook.IdWebhook, 
                    response.StatusCode, 
                    webhook.Url);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook {WebhookId} to {Url}", 
                webhook.IdWebhook, webhook.Url);
        }
    }

    private object FormatDiscordMessage(object payload)
    {
        // Extract error data from generic payload
        var payloadJson = JsonSerializer.Serialize(payload);
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;
        
        var error = root.GetProperty("error");
        var errorId = error.GetProperty("id").GetString();
        var message = error.GetProperty("message").GetString();
        var stackTrace = error.TryGetProperty("stackTrace", out var st) ? st.GetString() : null;
        var summary = error.TryGetProperty("summary", out var sum) ? sum.GetString() : null;
        var status = error.GetProperty("status").GetString();
        var createdAt = error.GetProperty("createdAt").GetDateTime();
        
        // Truncate stack trace for Discord (max 1024 chars per field)
        var truncatedStack = stackTrace?.Length > 1000 
            ? stackTrace.Substring(0, 1000) + "..." 
            : stackTrace;
        
        // Create Discord embed format
        return new
        {
            embeds = new[]
            {
                new
                {
                    title = "🚨 New Error Detected",
                    description = summary ?? message,
                    color = status == "Resolved" ? 3066993 : 15158332, // Green for resolved, red for new
                    fields = new[]
                    {
                        new { name = "Error Message", value = (string?)(message?.Length > 256 ? message.Substring(0, 253) + "..." : message), inline = false },
                        new { name = "Status", value = (string?)status, inline = true },
                        new { name = "Error ID", value = (string?)errorId, inline = true },
                        new { name = "Stack Trace", value = (string?)(string.IsNullOrEmpty(truncatedStack) ? "No stack trace available" : $"```{truncatedStack}```"), inline = false }
                    },
                    timestamp = createdAt.ToString("o"),
                    footer = new { text = "ShitCode Error Dashboard" }
                }
            }
        };
    }

    private object FormatTelegramMessage(object payload)
    {
        // Extract error data from generic payload
        var payloadJson = JsonSerializer.Serialize(payload);
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;
        
        var error = root.GetProperty("error");
        var errorId = error.GetProperty("id").GetString();
        var message = error.GetProperty("message").GetString();
        var stackTrace = error.TryGetProperty("stackTrace", out var st) ? st.GetString() : null;
        var summary = error.TryGetProperty("summary", out var sum) ? sum.GetString() : null;
        var status = error.GetProperty("status").GetString();
        var createdAt = error.GetProperty("createdAt").GetDateTime();
        
        // Truncate for Telegram (max 4096 chars)
        var truncatedStack = stackTrace?.Length > 500 
            ? stackTrace.Substring(0, 500) + "..." 
            : stackTrace;
        
        // Create Telegram message format with markdown
        var text = $@"🚨 *New Error Detected*

*Error Message:* {message}
*Status:* {status}
*Error ID:* `{errorId}`
*Time:* {createdAt:yyyy-MM-dd HH:mm:ss} UTC

{(string.IsNullOrEmpty(summary) ? "" : $"*Summary:* {summary}\n")}
*Stack Trace:*
```
{truncatedStack ?? "No stack trace available"}
```

_ShitCode Error Dashboard_";
        
        return new
        {
            text = text,
            parse_mode = "Markdown"
        };
    }

    public async Task<bool> TestWebhookAsync(string url, string? secretToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var testPayload = new
            {
                @event = "webhook.test",
                timestamp = DateTime.UtcNow,
                message = "This is a test webhook from ShitCode Error Dashboard"
            };

            var json = JsonSerializer.Serialize(testPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (!string.IsNullOrEmpty(secretToken))
            {
                client.DefaultRequestHeaders.Add("X-Webhook-Secret", secretToken);
            }

            var response = await client.PostAsync(url, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test webhook at {Url}", url);
            return false;
        }
    }
}
