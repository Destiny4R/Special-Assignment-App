using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpecialAssignment.Models;

public class LocationPoint
{
    [Key]
    public long Id { get; set; }

    public long ReportSessionId { get; set; }
    public ReportSession? ReportSession { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal Latitude { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal Longitude { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Accuracy { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Altitude { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Speed { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Heading { get; set; }

    public DateTime CapturedAt { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
