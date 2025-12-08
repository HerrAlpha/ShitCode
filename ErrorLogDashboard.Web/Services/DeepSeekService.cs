using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ErrorLogDashboard.Web.Services;

public class DeepSeekService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ApiEndpoint = "https://api.deepseek.com/v1/chat/completions";

    public DeepSeekService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _apiKey = configuration["DeepSeek:ApiKey"] ?? throw new InvalidOperationException("DeepSeek API key not configured");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<string?> GenerateErrorSummaryAsync(string errorMessage, string? stackTrace)
    {
        try
        {
            var prompt = BuildPrompt(errorMessage, stackTrace);
            var requestBody = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
                    new { role = "system", content = @"You are a helpful assistant that analyzes software errors. 
Analyze the error and provide your response in this EXACT JSON format:
{
  ""priority"": ""Low|Medium|High|Urgent"",
  ""summary"": ""Brief technical summary and actionable solutions""
}

Priority Guidelines:
- Low: Minor issues, warnings, deprecated APIs, cosmetic bugs
- Medium: Functional bugs that don't block core features, performance degradation
- High: Core functionality broken, data integrity issues, security vulnerabilities
- Urgent: Critical system failures, data loss, security breaches, production down

Keep the summary concise and developer-focused. Include what went wrong and 1-2 actionable solutions." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 200,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ApiEndpoint, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DeepSeekResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var aiContent = result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
            
            // Return the AI response (should be JSON, but return as-is)
            return aiContent;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DeepSeek API error: {ex.Message}");
            return null; // Return null if summary generation fails
        }
    }

    private string BuildPrompt(string errorMessage, string? stackTrace)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyze this error and provide a concise technical summary:");
        sb.AppendLine($"Error: {errorMessage}");
        
        if (!string.IsNullOrEmpty(stackTrace))
        {
            // Limit stack trace to first few lines to avoid token limits
            var stackLines = stackTrace.Split('\n').Take(5);
            sb.AppendLine($"Stack Trace (partial): {string.Join("\n", stackLines)}");
        }

        return sb.ToString();
    }
}

// Response models for DeepSeek API
public class DeepSeekResponse
{
    public Choice[]? Choices { get; set; }
}

public class Choice
{
    public Message? Message { get; set; }
}

public class Message
{
    public string? Content { get; set; }
}
