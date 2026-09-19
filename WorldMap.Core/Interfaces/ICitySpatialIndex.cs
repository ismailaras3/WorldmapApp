using WorldMap.Core.Models;

namespace WorldMap.Core.Interfaces;

public interface ICitySpatialIndex
{
    void Build(
        IEnumerable<City> cities);

    IReadOnlyList<City> Query(
        double minLongitude,
        double minLatitude,
        double maxLongitude,
        double maxLatitude);
}