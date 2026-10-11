using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SpecialAssignment.Data;
using SpecialAssignment.Models;

namespace SpecialAssignment.Pages.Incidents;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public ReportSession? Session { get; private set; }
    public List<LocationPoint> Points { get; private set; } = [];
    public DetailsModel(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> OnGetAsync(string id)
    {
        Session = await _db.ReportSessions.FirstOrDefaultAsync(s => s.PublicReportId == id);
        if (Session is null) return Page();
        Points = await _db.LocationPoints.Where(p => p.ReportSessionId == Session.Id).OrderBy(p => p.CapturedAt).Take(500).ToListAsync();
        return Page();
    }
}
