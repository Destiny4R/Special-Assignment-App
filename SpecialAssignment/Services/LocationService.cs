using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SpecialAssignment.Configuration;
using SpecialAssignment.Data;
using SpecialAssignment.DTOs;
using SpecialAssignment.Models;

namespace SpecialAssignment.Services;

public interface ILocationService
{
    Task<(bool Ok, string? Error, LocationPointDto? Point)> AppendAsync(string publicReportId, LocationUpdateRequest req);
    Task<List<LocationPointDto>> GetHistoryAsync(string publicReportId, int take = 200);
    Task<LocationPointDto?> GetLatestAsync(string publicReportId);
}

public class LocationService : ILocationService
{
    private readonly ApplicationDbContext _db;
    private readonly IGisService _gis;
    private readonly MonitoringOptions _options;

    public LocationService(ApplicationDbContext db, IGisService gis, IOptions<MonitoringOptions> options)
    {
        _db = db; _gis = gis; _options = options.Value;
    }

    public async Task<(bool Ok, string? Error, LocationPointDto? Point)> AppendAsync(string publicReportId, LocationUpdateRequest req)
    {
        if (req.Latitude is < -90 or > 90) return (false, "Latitude must be between -90 and +90.", null);
        if (req.Longitude is < -180 or > 180) return (false, "Longitude must be between -180 and +180.", null);
        if (req.Accuracy is < 0) return (false, "Accuracy must be >= 0.", null);

        var capturedAt = req.CapturedAt ?? DateTime.UtcNow;
        if (capturedAt > DateTime.UtcNow.AddMinutes(5)) return (false, "Timestamp is in the future.", null);
        if (capturedAt < DateTime.UtcNow.AddHours(-24)) return (false, "Timestamp is too old.", null);

        var session = await _db.ReportSessions.FirstOrDefaultAsync(s => s.PublicReportId == publicReportId);
        if (session is null) return (false, "Report ID does not exist.", null);
        if (session.Status == ReportStatus.Ended) return (false, "Session has ended.", null);

        // Movement threshold: skip redundant points (unless first point or stale)
        if (session.LastLatitude != null && session.LastLongitude != null && session.LastSeenAt != null)
        {
            var dist = _gis.HaversineMeters((double)session.LastLatitude, (double)session.LastLongitude, req.Latitude, req.Longitude);
            var elapsed = (DateTime.UtcNow - session.LastSeenAt.Value).TotalSeconds;
            if (dist < _options.MinimumMovementMeters && elapsed < _options.TargetUpdateIntervalSeconds)
            {
                session.LastSeenAt = DateTime.UtcNow;
                session.Status = ReportStatus.Live;
                await _db.SaveChangesAsync();
                return (true, null, ToDto(new LocationPoint
                {
                    Latitude = (decimal)req.Latitude,
                    Longitude = (decimal)req.Longitude,
                    Accuracy = req.Accuracy is null ? null : (decimal)req.Accuracy,
                    CapturedAt = capturedAt,
                    ReceivedAt = DateTime.UtcNow
                }));
            }
        }

        var point = new LocationPoint
        {
            ReportSessionId = session.Id,
            Latitude = (decimal)req.Latitude,
            Longitude = (decimal)req.Longitude,
            Accuracy = req.Accuracy is null ? null : (decimal)req.Accuracy,
            Altitude = req.Altitude is null ? null : (decimal)req.Altitude,
            Speed = req.Speed is null ? null : (decimal)req.Speed,
            Heading = req.Heading is null ? null : (decimal)req.Heading,
            CapturedAt = capturedAt,
            ReceivedAt = DateTime.UtcNow
        };
        _db.LocationPoints.Add(point);

        session.LastLatitude = point.Latitude;
        session.LastLongitude = point.Longitude;
        session.LastAccuracy = point.Accuracy;
        session.LastSeenAt = DateTime.UtcNow;
        session.Status = ReportStatus.Live;

        await _db.SaveChangesAsync();
        return (true, null, ToDto(point));
    }

    public async Task<List<LocationPointDto>> GetHistoryAsync(string publicReportId, int take = 200)
    {
        take = Math.Clamp(take, 1, 1000);
        var session = await _db.ReportSessions.FirstOrDefaultAsync(s => s.PublicReportId == publicReportId);
        if (session is null) return [];
        // NOTE: TakeLast() cannot be translated to SQL — take newest first, then reverse in memory.
        var newest = await _db.LocationPoints
            .Where(p => p.ReportSessionId == session.Id)
            .OrderByDescending(p => p.CapturedAt)
            .Take(take)
            .Select(p => new LocationPointDto
            {
                Latitude = (double)p.Latitude,
                Longitude = (double)p.Longitude,
                Accuracy = p.Accuracy == null ? null : (double)p.Accuracy,
                Speed = p.Speed == null ? null : (double)p.Speed,
                Heading = p.Heading == null ? null : (double)p.Heading,
                Altitude = p.Altitude == null ? null : (double)p.Altitude,
                CapturedAt = p.CapturedAt,
                ReceivedAt = p.ReceivedAt
            }).ToListAsync();
        newest.Reverse(); // chronological for the trail polyline
        return newest;
    }

    public async Task<LocationPointDto?> GetLatestAsync(string publicReportId)
    {
        var session = await _db.ReportSessions.FirstOrDefaultAsync(s => s.PublicReportId == publicReportId);
        if (session?.LastLatitude is null || session.LastLongitude is null) return null;
        var last = await _db.LocationPoints.Where(p => p.ReportSessionId == session.Id)
            .OrderByDescending(p => p.CapturedAt).FirstOrDefaultAsync();
        if (last is null)
            return new LocationPointDto
            {
                Latitude = (double)session.LastLatitude,
                Longitude = (double)session.LastLongitude,
                Accuracy = session.LastAccuracy == null ? null : (double)session.LastAccuracy,
                CapturedAt = session.LastSeenAt ?? DateTime.UtcNow,
                ReceivedAt = session.LastSeenAt ?? DateTime.UtcNow
            };
        return ToDto(last);
    }

    private static LocationPointDto ToDto(LocationPoint p) => new()
    {
        Latitude = (double)p.Latitude,
        Longitude = (double)p.Longitude,
        Accuracy = p.Accuracy == null ? null : (double)p.Accuracy,
        Speed = p.Speed == null ? null : (double)p.Speed,
        Heading = p.Heading == null ? null : (double)p.Heading,
        Altitude = p.Altitude == null ? null : (double)p.Altitude,
        CapturedAt = p.CapturedAt,
        ReceivedAt = p.ReceivedAt
    };
}
