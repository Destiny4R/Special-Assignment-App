using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using SpecialAssignment.DTOs;
using SpecialAssignment.Hubs;
using SpecialAssignment.Models;
using SpecialAssignment.Services;

namespace SpecialAssignment.Controllers;

// Public reporter API — no login required (location sharing must stay frictionless).
// Protected by validation + rate limiting, never by obscurity.
[ApiController]
[Route("api/report")]
[EnableRateLimiting("reporter")]
public class ReportApiController : ControllerBase
{
    private readonly IReportService _reports;
    private readonly ILocationService _locations;
    private readonly IRiskAssessmentService _risk;
    private readonly IHubContext<MonitoringHub> _hub;
    private readonly IConfiguration _config;

    public ReportApiController(
        IReportService reports, ILocationService locations,
        IRiskAssessmentService risk, IHubContext<MonitoringHub> hub,
        IConfiguration config)
    {
        _reports = reports; _locations = locations; _risk = risk; _hub = hub; _config = config;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartReportRequest req)
    {
        if (!req.ConsentAccepted)
            return BadRequest(new { error = "Consent must be accepted before tracking begins." });

        var termsVersion = req.TermsVersion
            ?? _config.GetValue<string>("Monitoring:TermsVersion") ?? "v1.0";

        var session = await _reports.StartAsync(
            termsVersion,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var payload = new
        {
            reportId = session.PublicReportId,
            status = session.Status,
            termsVersion,
            acceptedAt = session.ConsentAcceptedAt
        };

        await _hub.Clients.Group("dashboard").SendAsync("ReportStarted", payload);
        return Ok(payload);
    }

    [HttpPost("{reportId}/location")]
    public async Task<IActionResult> PostLocation(string reportId, [FromBody] LocationUpdateRequest req)
    {
        var (ok, error, point) = await _locations.AppendAsync(reportId, req);
        if (!ok) return BadRequest(new { error });

        var update = new
        {
            reportId,
            latitude = point!.Latitude,
            longitude = point.Longitude,
            accuracy = point.Accuracy,
            speed = point.Speed,
            heading = point.Heading,
            capturedAt = point.CapturedAt,
            receivedAt = point.ReceivedAt
        };

        await _hub.Clients.Group("dashboard").SendAsync("LocationUpdated", update);
        await _hub.Clients.Group($"report-{reportId}").SendAsync("LocationUpdated", update);

        var session = await _reports.GetByPublicIdAsync(reportId);
        if (session is not null)
        {
            await _risk.EvaluateIncomingLocationAsync(session, point.Latitude, point.Longitude);
            await _risk.EvaluateClustersAsync();
        }

        return Ok(update);
    }

    [HttpPost("{reportId}/stop")]
    public async Task<IActionResult> Stop(string reportId)
    {
        await _reports.EndAsync(reportId);
        await _hub.Clients.Group("dashboard").SendAsync("ReportEnded", new { reportId });
        await _hub.Clients.Group($"report-{reportId}").SendAsync("ReportEnded", new { reportId });
        return Ok(new { reportId, status = ReportStatus.Ended });
    }
}
