using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SpecialAssignment.Configuration;
using SpecialAssignment.Data;
using SpecialAssignment.DTOs;
using SpecialAssignment.Models;

namespace SpecialAssignment.Services;

public interface IReportService
{
    Task<ReportSession> StartAsync(string termsVersion, string? ip, string? userAgent);
    Task<ReportSession?> GetByPublicIdAsync(string publicReportId);
    Task EndAsync(string publicReportId);
    Task<List<LiveReportDto>> GetActiveAsync();
    Task TouchStaleAsync();
}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;
    private readonly MonitoringOptions _options;

    public ReportService(ApplicationDbContext db, IOptions<MonitoringOptions> options)
    {
        _db = db; _options = options.Value;
    }

    public async Task<ReportSession> StartAsync(string termsVersion, string? ip, string? userAgent)
    {
        var session = new ReportSession
        {
            PublicReportId = $"REP-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            Status = ReportStatus.Live,
            ConsentAccepted = true,
            ConsentVersion = termsVersion,
            ConsentAcceptedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _db.ReportSessions.Add(session);
        await _db.SaveChangesAsync();

        _db.ConsentRecords.Add(new ConsentRecord
        {
            ReportSessionId = session.Id,
            TermsVersion = termsVersion,
            Accepted = true,
            AcceptedAt = DateTime.UtcNow,
            IpAddress = ip,
            UserAgent = userAgent
        });
        await _db.SaveChangesAsync();
        return session;
    }

    public Task<ReportSession?> GetByPublicIdAsync(string publicReportId)
        => _db.ReportSessions
            .Include(s => s.LocationPoints.OrderByDescending(p => p.CapturedAt).Take(200))
            .FirstOrDefaultAsync(s => s.PublicReportId == publicReportId);

    public async Task EndAsync(string publicReportId)
    {
        var session = await _db.ReportSessions.FirstOrDefaultAsync(s => s.PublicReportId == publicReportId);
        if (session is null || session.Status == ReportStatus.Ended) return;
        session.Status = ReportStatus.Ended;
        session.EndedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<List<LiveReportDto>> GetActiveAsync()
    {
        var sessions = await _db.ReportSessions
            .Where(s => s.Status != ReportStatus.Ended)
            .OrderByDescending(s => s.LastSeenAt)
            .Take(100)
            .ToListAsync();
        return sessions.Select(ToLiveDto).ToList();
    }

    public async Task TouchStaleAsync()
    {
        var now = DateTime.UtcNow;
        var staleAfter = now.AddSeconds(-_options.StaleThresholdSeconds);
        var offlineAfter = now.AddSeconds(-_options.OfflineThresholdSeconds);

        var candidates = await _db.ReportSessions
            .Where(s => s.Status == ReportStatus.Live || s.Status == ReportStatus.Stale || s.Status == ReportStatus.Offline)
            .ToListAsync();

        foreach (var s in candidates)
        {
            if (s.LastSeenAt is null || s.LastSeenAt < offlineAfter)
                s.Status = ReportStatus.Offline;
            else if (s.LastSeenAt < staleAfter)
                s.Status = ReportStatus.Stale;
            else
                s.Status = ReportStatus.Live;
        }
        await _db.SaveChangesAsync();
    }

    public static LiveReportDto ToLiveDto(ReportSession s)
    {
        var status = s.Status;
        var display = status switch
        {
            ReportStatus.Live => "LIVE",
            ReportStatus.Stale => "STALE",
            ReportStatus.Offline => "OFFLINE",
            ReportStatus.Ended => "ENDED",
            _ => status
        };
        return new LiveReportDto
        {
            ReportId = s.PublicReportId,
            Status = status,
            DisplayStatus = display,
            Latitude = s.LastLatitude is null ? null : (double)s.LastLatitude,
            Longitude = s.LastLongitude is null ? null : (double)s.LastLongitude,
            Accuracy = s.LastAccuracy is null ? null : (double)s.LastAccuracy,
            StartedAt = s.StartedAt,
            LastSeenAt = s.LastSeenAt,
            LastSeenAgo = s.LastSeenAt is null ? "never" : HumanizeAgo(DateTime.UtcNow - s.LastSeenAt.Value),
            IsDelayed = false
        };
    }

    private static string HumanizeAgo(TimeSpan ts)
    {
        if (ts.TotalSeconds < 10) return "just now";
        if (ts.TotalSeconds < 60) return $"{(int)ts.TotalSeconds} sec ago";
        if (ts.TotalMinutes < 60) return $"{(int)ts.TotalMinutes} min ago";
        return $"{(int)ts.TotalHours} hr ago";
    }
}
