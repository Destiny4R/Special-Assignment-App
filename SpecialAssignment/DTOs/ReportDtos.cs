using System.ComponentModel.DataAnnotations;

namespace SpecialAssignment.DTOs;

public class StartReportRequest
{
    [Required]
    public bool ConsentAccepted { get; set; }

    [MaxLength(30)]
    public string? TermsVersion { get; set; }
}

public class LocationUpdateRequest
{
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Range(0, 100000)]
    public double? Accuracy { get; set; }

    public double? Altitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public DateTime? CapturedAt { get; set; }
}

public class LiveReportDto
{
    public string ReportId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DisplayStatus { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Accuracy { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string LastSeenAgo { get; set; } = string.Empty;
    public bool IsDelayed { get; set; }
}

public class LocationPointDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public double? Altitude { get; set; }
    public DateTime CapturedAt { get; set; }
    public DateTime ReceivedAt { get; set; }
}

public class AlertDto
{
    public long Id { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
