namespace WorldMap.Core.Models;

public sealed class GeoPolygon
{
    public IReadOnlyList<GeoCoordinate> OuterRing { get; }

    public IReadOnlyList<IReadOnlyList<GeoCoordinate>> Holes { get; }

    public GeoPolygon(
        IReadOnlyList<GeoCoordinate> outerRing,
        IReadOnlyList<IReadOnlyList<GeoCoordinate>>? holes = null)
    {
        OuterRing = outerRing;
        Holes = holes ?? [];
    }
}