using WorldMap.Core.Models;

namespace WorldMap.Core.Projections;

public interface IMapProjection
{
    string Name { get; }

    ProjectedPoint Project(GeoCoordinate coordinate);

    GeoCoordinate Unproject(ProjectedPoint point);
}