using System.ComponentModel.DataAnnotations;

namespace SpecialAssignment.Models;

public class AuditLog
{
    [Key]
    public long Id { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    public long? ReportSessionId { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
