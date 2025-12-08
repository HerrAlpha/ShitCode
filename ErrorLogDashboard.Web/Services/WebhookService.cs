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

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add secret token if configured
            if (!string.IsNullOrEmpty(webhook.SecretToken))
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
