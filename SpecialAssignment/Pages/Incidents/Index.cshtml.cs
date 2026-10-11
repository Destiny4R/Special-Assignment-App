using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SpecialAssignment.Data;
using SpecialAssignment.Models;

namespace SpecialAssignment.Pages.Incidents;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public List<ReportSession> Sessions { get; private set; } = [];
    public IndexModel(ApplicationDbContext db) => _db = db;
    public async Task OnGetAsync()
        => Sessions = await _db.ReportSessions.OrderByDescending(s => s.StartedAt).Take(100).ToListAsync();
}
