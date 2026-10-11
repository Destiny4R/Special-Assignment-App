using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using SpecialAssignment.Configuration;

namespace SpecialAssignment.Pages.Report;

public class IndexModel : PageModel
{
    public string TermsVersion { get; private set; } = "v1.0";
    private readonly MonitoringOptions _options;

    public IndexModel(IOptions<MonitoringOptions> options) => _options = options.Value;

    public void OnGet() => TermsVersion = _options.TermsVersion;
}
