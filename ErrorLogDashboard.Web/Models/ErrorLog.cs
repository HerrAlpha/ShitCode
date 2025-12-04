using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErrorLogDashboard.Web.Models;

public class ErrorLog
{
    [Key]
    public Guid IdErrorLog { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public string? StackTrace { get; set; }
    public string? Summary { get; set; } // AI-generated summary
    
    public Guid IdProject { get; set; }
    [ForeignKey("IdProject")]
    public Project? Project { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
