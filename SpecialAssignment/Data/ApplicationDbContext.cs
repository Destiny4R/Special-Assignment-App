using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpecialAssignment.Models;

namespace SpecialAssignment.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ReportSession> ReportSessions => Set<ReportSession>();
    public DbSet<LocationPoint> LocationPoints => Set<LocationPoint>();
    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<RiskZone> RiskZones => Set<RiskZone>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ReportSession>(e =>
        {
            e.HasIndex(x => x.PublicReportId).IsUnique();
            e.HasIndex(x => x.Status);
            e.Property(x => x.PublicReportId).HasMaxLength(50).IsRequired();
        });

        builder.Entity<LocationPoint>(e =>
        {
            e.HasIndex(x => x.ReportSessionId);
            e.HasIndex(x => x.CapturedAt);
            e.HasOne(x => x.ReportSession)
                .WithMany(r => r.LocationPoints)
                .HasForeignKey(x => x.ReportSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ConsentRecord>(e =>
        {
            e.HasOne(x => x.ReportSession)
                .WithMany(r => r.ConsentRecords)
                .HasForeignKey(x => x.ReportSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Alert>(e =>
        {
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Severity);
            e.HasIndex(x => x.CreatedAt);
        });

        builder.Entity<RiskZone>(e => { e.HasIndex(x => x.IsActive); });

        builder.Entity<AuditLog>(e => { e.HasIndex(x => x.CreatedAt); });
    }
}
