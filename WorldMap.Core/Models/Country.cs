namespace WorldMap.Core.Models;

public sealed class Country
{
    public string Name { get; }

    public string Continent { get; }

    public IReadOnlyList<GeoPolygon> Polygons { get; }

    public Country(
        string name,
        IReadOnlyList<GeoPolygon> polygons,
        string continent = "Unknown")
    {
        Name = name;
        Polygons = polygons;
        Continent = continent;
    }
}