using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SpecialAssignment.Data;
using SpecialAssignment.Models;
using SpecialAssignment.Services;

namespace SpecialAssignment.Pages.Alerts;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IAlertService _alerts;
    public List<Alert> Alerts { get; private set; } = [];
    public IndexModel(ApplicationDbContext db, IAlertService alerts) { _db = db; _alerts = alerts; }

    public async Task OnGetAsync()
        => Alerts = await _db.Alerts.OrderByDescending(a => a.CreatedAt).Take(50).ToListAsync();

    public async Task<IActionResult> OnPostAcknowledgeAsync(long id)
    {
        await _alerts.AcknowledgeAsync(id, User.Identity?.Name ?? "operator");
        return RedirectToPage();
    }
}
