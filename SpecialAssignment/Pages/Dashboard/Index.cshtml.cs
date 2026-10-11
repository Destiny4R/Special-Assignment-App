using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using SpecialAssignment.Configuration;

namespace SpecialAssignment.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    public string MapsKey { get; private set; } = string.Empty;
    public double DefaultLat { get; private set; }
    public double DefaultLon { get; private set; }
    public int DefaultZoom { get; private set; }

    public IndexModel(IOptions<GoogleMapsOptions> maps) { MapsKey = maps.Value.ApiKey; DefaultLat = maps.Value.DefaultLatitude; DefaultLon = maps.Value.DefaultLongitude; DefaultZoom = maps.Value.DefaultZoom; }
    public void OnGet() { }
}
