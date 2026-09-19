namespace WorldMap.Core.Models;

public readonly record struct GeoCoordinate(
    double Longitude,
    double Latitude)
{
    public override string ToString()
    {
        return $"Lon: {Longitude:F6}°, Lat: {Latitude:F6}°";
    }
}