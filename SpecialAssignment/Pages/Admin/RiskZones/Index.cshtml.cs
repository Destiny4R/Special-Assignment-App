using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SpecialAssignment.Data;
using SpecialAssignment.Models;

namespace SpecialAssignment.Pages.Admin.RiskZones;

[Authorize(Roles = "System Administrator,Operations Manager,GIS Analyst")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public List<RiskZone> Zones { get; private set; } = [];
    [BindProperty] public RiskZone Input { get; set; } = new();
    public IndexModel(ApplicationDbContext db) => _db = db;

    public async Task OnGetAsync()
        => Zones = await _db.RiskZones.OrderBy(z => z.Name).ToListAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        _db.RiskZones.Add(new RiskZone
        {
            Name = Input.Name, RiskLevel = Input.RiskLevel,
            CenterLatitude = Input.CenterLatitude, CenterLongitude = Input.CenterLongitude,
            RadiusMeters = Input.RadiusMeters <= 0 ? 1000 : Input.RadiusMeters,
            IsActive = true, CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(long id)
    {
        var z = await _db.RiskZones.FindAsync(id);
        if (z is not null) { z.IsActive = !z.IsActive; await _db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}
