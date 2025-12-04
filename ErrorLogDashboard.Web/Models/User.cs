using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErrorLogDashboard.Web.Models;

public class User
{
    [Key]
    public Guid IdUser { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public string Role { get; set; } = "User"; // Admin, User

    public Guid IdSubscriptionPlan { get; set; }
    [ForeignKey("IdSubscriptionPlan")]
    public SubscriptionPlan? SubscriptionPlan { get; set; }
    
    public List<Project> Projects { get; set; } = new();
}
