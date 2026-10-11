namespace SpecialAssignment.Configuration;

public class MonitoringOptions
{
    public int TargetUpdateIntervalSeconds { get; set; } = 8;
    public double MinimumMovementMeters { get; set; } = 15;
    public int StaleThresholdSeconds { get; set; } = 45;
    public int OfflineThresholdSeconds { get; set; } = 180;
    public string TermsVersion { get; set; } = "v1.0";
    public int MaxLocationsPerRequest { get; set; } = 50;
    public int RateLimitPerMinute { get; set; } = 30;
}

public class EarlyWarningOptions
{
    public int ClusterCountWarning { get; set; } = 3;
    public int ClusterCountCritical { get; set; } = 5;
    public double ClusterRadiusMeters { get; set; } = 1000;
    public int ClusterWindowMinutes { get; set; } = 5;
}

public class GoogleMapsOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public double DefaultLatitude { get; set; } = 9.0765;
    public double DefaultLongitude { get; set; } = 7.3986;
    public int DefaultZoom { get; set; } = 12;
}
