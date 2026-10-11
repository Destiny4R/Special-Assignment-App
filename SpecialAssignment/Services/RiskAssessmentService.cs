using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SpecialAssignment.Configuration;
using SpecialAssignment.Data;
using SpecialAssignment.DTOs;
using SpecialAssignment.Models;

namespace SpecialAssignment.Services;

public interface IRiskAssessmentService
{
    Task EvaluateIncomingLocationAsync(ReportSession session, double lat, double lon);
    Task EvaluateClustersAsync();
}

public class RiskAssessmentService : IRiskAssessmentService
{
    private readonly ApplicationDbContext _db;
    private readonly IGisService _gis;
    private readonly IAlertService _alerts;
    private readonly EarlyWarningOptions _options;

    public RiskAssessmentService(
        ApplicationDbContext db, IGisService gis, IAlertService alerts,
        IOptions<EarlyWarningOptions> options)
    {
        _db = db; _gis = gis; _alerts = alerts; _options = options.Value;
    }

    // Rule B — high-risk zone entry
    public async Task EvaluateIncomingLocationAsync(ReportSession session, double lat, double lon)
    {
        var zones = await _db.RiskZones.Where(z => z.IsActive).ToListAsync();
        foreach (var zone in zones)
        {
            if (!_gis.IsWithinRadius(lat, lon,
                    (double)zone.CenterLatitude, (double)zone.CenterLongitude,
                    (double)zone.RadiusMeters))
                continue;

            var severity = zone.RiskLevel switch
            {
                RiskLevel.Critical => AlertSeverity.Critical,
                RiskLevel.High => AlertSeverity.High,
                RiskLevel.Medium => AlertSeverity.Warning,
                _ => AlertSeverity.Notice
            };

            var recent = await _db.Alerts.AnyAsync(a =>
                a.ReportSessionId == session.Id &&
                a.AlertType == "RISK_ZONE_ENTRY" &&
                a.CreatedAt > DateTime.UtcNow.AddMinutes(-15) &&
                a.Status == AlertStatus.Active);
            if (recent) continue;

            await _alerts.CreateAsync(
                "RISK_ZONE_ENTRY", severity,
                $"Reporter {session.PublicReportId} entered {zone.Name}",
                $"Live reporter entered {zone.RiskLevel} risk zone '{zone.Name}' (radius {(double)zone.RadiusMeters:N0} m).",
                session.Id, lat, lon);
        }
    }

    // Rule A + C — cluster detection across active sessions
    public async Task EvaluateClustersAsync()
    {
        var windowStart = DateTime.UtcNow.AddMinutes(-_options.ClusterWindowMinutes);
        var actives = await _db.ReportSessions
            .Where(s => s.Status == ReportStatus.Live &&
                        s.LastLatitude != null && s.LastLongitude != null &&
                        s.LastSeenAt != null && s.LastSeenAt >= windowStart)
            .ToListAsync();

        if (actives.Count < _options.ClusterCountWarning) return;

        var seen = new HashSet<long>();
        foreach (var anchor in actives)
        {
            if (seen.Contains(anchor.Id)) continue;
            var cluster = actives.Where(other =>
                _gis.HaversineMeters(
                    (double)anchor.LastLatitude!, (double)anchor.LastLongitude!,
                    (double)other.LastLatitude!, (double)other.LastLongitude!)
                <= _options.ClusterRadiusMeters).ToList();

            foreach (var m in cluster) seen.Add(m.Id);

            if (cluster.Count >= _options.ClusterCountCritical)
            {
                var dup = await _db.Alerts.AnyAsync(a =>
                    a.AlertType == "CRITICAL_CLUSTER" && a.Status == AlertStatus.Active &&
                    a.CreatedAt > DateTime.UtcNow.AddMinutes(-_options.ClusterWindowMinutes));
                if (!dup)
                    await _alerts.CreateAsync("CRITICAL_CLUSTER", AlertSeverity.Critical,
                        $"{cluster.Count} live reports clustered within {(_options.ClusterRadiusMeters / 1000):0.0} km",
                        $"{cluster.Count} live reporters detected within {(_options.ClusterRadiusMeters):N0} m in the last {_options.ClusterWindowMinutes} minutes.",
                        null, (double)anchor.LastLatitude!, (double)anchor.LastLongitude!);
            }
            else if (cluster.Count >= _options.ClusterCountWarning)
            {
                var dup = await _db.Alerts.AnyAsync(a =>
                    a.AlertType == "WARNING_CLUSTER" && a.Status == AlertStatus.Active &&
                    a.CreatedAt > DateTime.UtcNow.AddMinutes(-_options.ClusterWindowMinutes));
                if (!dup)
                    await _alerts.CreateAsync("WARNING_CLUSTER", AlertSeverity.Warning,
                        $"{cluster.Count} live reports detected nearby",
                        $"{cluster.Count} live reporters within {(_options.ClusterRadiusMeters):N0} m in the last {_options.ClusterWindowMinutes} minutes.",
                        null, (double)anchor.LastLatitude!, (double)anchor.LastLongitude!);
            }
        }
    }
}
