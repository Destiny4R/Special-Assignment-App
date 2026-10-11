using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpecialAssignment.Data;
using SpecialAssignment.Models;
using SpecialAssignment.Services;

namespace SpecialAssignment.Controllers;

// Authorized operations API — dashboard, map, incidents, alerts.
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardApiController : ControllerBase
{
    private readonly IReportService _reports;
    private readonly ILocationService _locations;
    private readonly IAlertService _alerts;
    private readonly ApplicationDbContext _db;

    public DashboardApiController(
        IReportService reports, ILocationService locations,
        IAlertService alerts, ApplicationDbContext db)
    {
        _reports = reports; _locations = locations; _alerts = alerts; _db = db;
    }

    [HttpGet("reports/active")]
    public async Task<IActionResult> Active()
    {
        await _reports.TouchStaleAsync();
        return Ok(await _reports.GetActiveAsync());
    }

    [HttpGet("reports/{reportId}/locations")]
    public async Task<IActionResult> History(string reportId, [FromQuery] int take = 200)
    {
        var history = await _locations.GetHistoryAsync(reportId, take);
        return Ok(history);
    }

    [HttpGet("reports/{reportId}/location/latest")]
    public async Task<IActionResult> Latest(string reportId)
    {
        var latest = await _locations.GetLatestAsync(reportId);
        if (latest is null) return NotFound(new { error = "No location available." });
        return Ok(latest);
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> Alerts()
        => Ok(await _alerts.GetActiveAsync(30));

    [HttpPost("alerts/{id:long}/acknowledge")]
    public async Task<IActionResult> Acknowledge(long id)
    {
        await _alerts.AcknowledgeAsync(id, User.Identity?.Name ?? "operator");
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            Action = "ALERT_ACK",
            Description = $"Acknowledged alert {id}"
        });
        await _db.SaveChangesAsync();
        return Ok(new { id, status = AlertStatus.Acknowledged });
    }
}
