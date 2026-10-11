using Microsoft.EntityFrameworkCore;
using SpecialAssignment.Data;
using SpecialAssignment.Models;

namespace SpecialAssignment.Services;

public interface IAlertService
{
    Task<Alert> CreateAsync(string type, string severity, string title, string message, long? sessionId = null, double? lat = null, double? lon = null);
    Task<List<Alert>> GetActiveAsync(int take = 20);
    Task AcknowledgeAsync(long id, string userName);
    Task ResolveAsync(long id);
}

public class AlertService : IAlertService
{
    private readonly ApplicationDbContext _db;
    public AlertService(ApplicationDbContext db) => _db = db;

    public async Task<Alert> CreateAsync(string type, string severity, string title, string message, long? sessionId = null, double? lat = null, double? lon = null)
    {
        var alert = new Alert
        {
            AlertType = type,
            Severity = severity,
            Title = title,
            Message = message,
            ReportSessionId = sessionId,
            Latitude = lat is null ? null : (decimal)lat,
            Longitude = lon is null ? null : (decimal)lon,
            Status = AlertStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _db.Alerts.Add(alert);
        await _db.SaveChangesAsync();
        return alert;
    }

    public Task<List<Alert>> GetActiveAsync(int take = 20)
        => _db.Alerts.Where(a => a.Status == AlertStatus.Active)
            .OrderByDescending(a => a.CreatedAt).Take(take).ToListAsync();

    public async Task AcknowledgeAsync(long id, string userName)
    {
        var alert = await _db.Alerts.FindAsync(id);
        if (alert is null) return;
        alert.Status = AlertStatus.Acknowledged;
        alert.AcknowledgedAt = DateTime.UtcNow;
        alert.AcknowledgedBy = userName;
        await _db.SaveChangesAsync();
    }

    public async Task ResolveAsync(long id)
    {
        var alert = await _db.Alerts.FindAsync(id);
        if (alert is null) return;
        alert.Status = AlertStatus.Resolved;
        await _db.SaveChangesAsync();
    }
}
