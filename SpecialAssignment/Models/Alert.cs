using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpecialAssignment.Models;

public static class AlertSeverity
{
    public const string Info = "INFO";
    public const string Notice = "NOTICE";
    public const string Warning = "WARNING";
    public const string High = "HIGH";
    public const string Critical = "CRITICAL";
}

public static class AlertStatus
{
    public const string Active = "ACTIVE";
    public const string Acknowledged = "ACKNOWLEDGED";
    public const string Resolved = "RESOLVED";
}

public class Alert
{
    [Key]
    public long Id { get; set; }

    [MaxLength(50)]
    public string AlertType { get; set; } = "GENERAL";

    [MaxLength(20)]
    public string Severity { get; set; } = AlertSeverity.Info;

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public long? ReportSessionId { get; set; }
    public ReportSession? ReportSession { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal? Longitude { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = AlertStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }

    [MaxLength(100)]
    public string? AcknowledgedBy { get; set; }
}
