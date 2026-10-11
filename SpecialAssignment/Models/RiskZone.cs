using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpecialAssignment.Models;

public static class RiskLevel
{
    public const string Low = "LOW";
    public const string Medium = "MEDIUM";
    public const string High = "HIGH";
    public const string Critical = "CRITICAL";
}

public class RiskZone
{
    [Key]
    public long Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string RiskLevel { get; set; } = Models.RiskLevel.Medium;

    [Column(TypeName = "decimal(10,7)")]
    public decimal CenterLatitude { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal CenterLongitude { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal RadiusMeters { get; set; } = 1000;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
