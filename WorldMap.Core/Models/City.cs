namespace WorldMap.Core.Models;

public sealed class City
{
    public long OsmId { get; init; }

    public string Name { get; init; } = "";

    public double Longitude { get; init; }

    public double Latitude { get; init; }

    public string PlaceType { get; init; } = "";

    public long? Population { get; init; }

    public bool IsCapital { get; init; }

    public GeoCoordinate Coordinate =>
        new GeoCoordinate(
            Longitude,
            Latitude);

    public override string ToString()
    {
        return
            $"{Name} ({PlaceType}) - " +
            $"{Longitude:F4}, {Latitude:F4}";
    }
}