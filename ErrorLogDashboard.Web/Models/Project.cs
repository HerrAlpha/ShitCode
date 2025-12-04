using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErrorLogDashboard.Web.Models;

public class Project
{
    [Key]
    public Guid IdProject { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string SecurityKey { get; set; } = string.Empty;

    [Required]
    public string TechStack { get; set; } = string.Empty;
    
    public Guid IdUser { get; set; }
    [ForeignKey("IdUser")]
    public User? User { get; set; }
    
    public List<ErrorLog> ErrorLogs { get; set; } = new();
}
