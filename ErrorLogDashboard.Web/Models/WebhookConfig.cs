using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErrorLogDashboard.Web.Models;

public class WebhookConfig
{
    [Key]
    public Guid IdWebhook { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Url { get; set; } = string.Empty;
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? SecretToken { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public Guid IdProject { get; set; }
    [ForeignKey("IdProject")]
    public Project? Project { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
