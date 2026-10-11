using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpecialAssignment.Models;

namespace SpecialAssignment.Data;

public static class AppRoles
{
    public const string Admin = "System Administrator";
    public const string Manager = "Operations Manager";
    public const string Officer = "Response Officer";
    public const string Analyst = "GIS Analyst";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [Admin, Manager, Officer, Analyst, Viewer];
}

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var context = sp.GetRequiredService<ApplicationDbContext>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        try
        {
            if (context.Database.IsRelational())
                await context.Database.MigrateAsync();
            else
                await context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database migration failed; continuing with EnsureCreated.");
            try { await context.Database.EnsureCreatedAsync(); } catch { }
        }

        try
        {
            foreach (var role in AppRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            if (!await context.RiskZones.AnyAsync())
            {
                context.RiskZones.AddRange(
                    new RiskZone { Name = "Grazing Corridor A — High Risk", RiskLevel = RiskLevel.High, CenterLatitude = 9.0765m, CenterLongitude = 7.3986m, RadiusMeters = 1500, IsActive = true },
                    new RiskZone { Name = "River Crossing — Medium", RiskLevel = RiskLevel.Medium, CenterLatitude = 9.0900m, CenterLongitude = 7.4100m, RadiusMeters = 1000, IsActive = true },
                    new RiskZone { Name = "Market Route — Watch", RiskLevel = RiskLevel.Low, CenterLatitude = 9.0600m, CenterLongitude = 7.3800m, RadiusMeters = 2000, IsActive = true }
                );
                await context.SaveChangesAsync();
            }

            const string adminEmail = "admin@earlywarning.local";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    DisplayName = "System Administrator"
                };
                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
            }
        }
        catch (Exception ex)
        {
            // MySQL down? App still starts; API calls will surface the DB error.
            // Run with USE_INMEMORY=true for a dependency-free demo.
            logger.LogError(ex, "Database seeding failed. If MySQL is unavailable, restart with USE_INMEMORY=true.");
        }
    }
}
