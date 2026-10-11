using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SpecialAssignment.Configuration;
using SpecialAssignment.Data;
using SpecialAssignment.Hubs;
using SpecialAssignment.Models;
using SpecialAssignment.Services;

var builder = WebApplication.CreateBuilder(args);

// Options (§13, §16 thresholds are configurable, not hard-coded)
builder.Services.Configure<MonitoringOptions>(builder.Configuration.GetSection("Monitoring"));
builder.Services.Configure<EarlyWarningOptions>(builder.Configuration.GetSection("EarlyWarning"));
builder.Services.Configure<GoogleMapsOptions>(builder.Configuration.GetSection("GoogleMaps"));

// Database: MySQL (Pomelo) required by baseline; InMemory fallback for local demo without MySQL.
// Set USE_INMEMORY=true env var or "UseInMemory": true config to force the fallback.
var mySqlConn = builder.Configuration.GetConnectionString("DefaultConnection");
var forceInMemory = builder.Configuration.GetValue<bool>("UseInMemory")
    || string.Equals(Environment.GetEnvironmentVariable("USE_INMEMORY"), "true", StringComparison.OrdinalIgnoreCase);
if (!forceInMemory && !string.IsNullOrWhiteSpace(mySqlConn))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(mySqlConn, ServerVersion.AutoDetect(mySqlConn)));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("EarlyWarningDemo"));
}

// Identity (§19)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// APIs under /api and /hubs must return 401/403 (not login redirects) so fetch() clients behave correctly.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/hubs"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/hubs"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// App services (§15–§16 isolated from Razor Pages)
builder.Services.AddScoped<IGisService, GisService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IRiskAssessmentService, RiskAssessmentService>();

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Rate limiting (§20, §23)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("reporter", o =>
    {
        o.PermitLimit = builder.Configuration.GetValue<int>("Monitoring:RateLimitPerMinute", 30);
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHub<MonitoringHub>("/hubs/monitoring");

await DbInitializer.SeedAsync(app.Services);

app.Run();
