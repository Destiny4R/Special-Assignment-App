using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SpecialAssignment.Data;

namespace SpecialAssignment.Pages.Incidents;

[Authorize]
public class HistoryModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public List<(string Id, string Started, string Ended, int Points)> Rows { get; private set; } = [];
    public HistoryModel(ApplicationDbContext db) => _db = db;

    public async Task OnGetAsync()
    {
        var sessions = await _db.ReportSessions.OrderByDescending(s => s.StartedAt).Take(200).ToListAsync();
        foreach (var s in sessions)
        {
            var n = await _db.LocationPoints.CountAsync(p => p.ReportSessionId == s.Id);
            Rows.Add((s.PublicReportId, s.StartedAt.ToString("u"), s.EndedAt?.ToString("u") ?? "—", n));
        }
    }
}
