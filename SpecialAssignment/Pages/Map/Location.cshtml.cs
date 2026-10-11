using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using SpecialAssignment.Configuration;

namespace SpecialAssignment.Pages.Map;

[Authorize]
public class LocationModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string ReportId { get; set; } = string.Empty;
    public string MapsKey { get; private set; } = string.Empty;
    public double DefaultLat { get; private set; }
    public double DefaultLon { get; private set; }

    public LocationModel(IOptions<GoogleMapsOptions> maps) { MapsKey = maps.Value.ApiKey; DefaultLat = maps.Value.DefaultLatitude; DefaultLon = maps.Value.DefaultLongitude; }

    public IActionResult OnGet()
    {
        if (string.IsNullOrWhiteSpace(ReportId)) return RedirectToPage("/Dashboard/Index");
        return Page();
    }
}
