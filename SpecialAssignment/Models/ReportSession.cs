using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpecialAssignment.Models;

public static class ReportStatus
{
    public const string Live = "LIVE";
    public const string Stale = "STALE";
    public const string Offline = "OFFLINE";
    public const string Ended = "ENDED";
}

public class ReportSession
{
    [Key]
    public long Id { get; set; }

    [Required, MaxLength(50)]
    public string PublicReportId { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Status { get; set; } = ReportStatus.Live;

    public bool ConsentAccepted { get; set; }

    [MaxLength(30)]
    public string ConsentVersion { get; set; } = "v1.0";

    public DateTime? ConsentAcceptedAt { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal? LastLatitude { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal? LastLongitude { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? LastAccuracy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LocationPoint> LocationPoints { get; set; } = new List<LocationPoint>();
    public ICollection<ConsentRecord> ConsentRecords { get; set; } = new List<ConsentRecord>();
}
