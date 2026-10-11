namespace SpecialAssignment.Services;

public interface IGisService
{
    double HaversineMeters(double lat1, double lon1, double lat2, double lon2);
    bool IsWithinRadius(double lat, double lon, double centerLat, double centerLon, double radiusMeters);
}

public class GisService : IGisService
{
    private const double EarthRadiusMeters = 6_371_000;

    public double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        return EarthRadiusMeters * c;
    }

    public bool IsWithinRadius(double lat, double lon, double centerLat, double centerLon, double radiusMeters)
        => HaversineMeters(lat, lon, centerLat, centerLon) <= radiusMeters;

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}
