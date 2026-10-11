using System.ComponentModel.DataAnnotations;

namespace SpecialAssignment.Models;

public class ConsentRecord
{
    [Key]
    public long Id { get; set; }

    public long ReportSessionId { get; set; }
    public ReportSession? ReportSession { get; set; }

    [MaxLength(30)]
    public string TermsVersion { get; set; } = "v1.0";

    public bool Accepted { get; set; }
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
