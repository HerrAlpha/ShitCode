using System.ComponentModel.DataAnnotations;

namespace ErrorLogDashboard.Web.Models;

public class SubscriptionPlan
{
    [Key]
    public Guid IdSubscriptionPlan { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public decimal Price { get; set; }
    
    public string Description { get; set; } = string.Empty;

    public int MaxProjects { get; set; }
    public int MaxDailyLogs { get; set; }
}
