using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using WorldMap.Core.Interfaces;
using WorldMap.Core.Models;

namespace WorldMap.Infrastructure.Spatial;

public sealed class CitySpatialIndex :
    ICitySpatialIndex
{
    private STRtree<City>? _tree;

    public void Build(
        IEnumerable<City> cities)
    {
        var tree =
            new STRtree<City>();

        foreach (City city in cities)
        {
            var envelope =
                new Envelope(
                    city.Longitude,
                    city.Longitude,
                    city.Latitude,
                    city.Latitude);

            tree.Insert(
                envelope,
                city);
        }

        tree.Build();

        _tree = tree;
    }

    public IReadOnlyList<City> Query(
        double minLongitude,
        double minLatitude,
        double maxLongitude,
        double maxLatitude)
    {
        if (_tree is null)
        {
            return Array.Empty<City>();
        }

        var searchEnvelope =
            new Envelope(
                minLongitude,
                maxLongitude,
                minLatitude,
                maxLatitude);

        return _tree
            .Query(searchEnvelope)
            .ToList();
    }
}