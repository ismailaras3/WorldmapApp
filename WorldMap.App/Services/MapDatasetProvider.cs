using WorldMap.Core.Models;

namespace WorldMap.App.Services;

public sealed class MapDatasetProvider
{//levert de lijst met beschikbare kaartdatasets aan de applicatie zodat de main zelf niets aanmaakt lever ik het zo 
    public IReadOnlyList<MapDataset> GetDatasets()
    {
        return
        [
            new MapDataset
        {
            Name = "Original",
            FileName = "ne_50m_admin_0_countries.geojson",
            Description = "Geen polygon reduction",
            PointCount = 99566,
            ReductionPercent = 0.0,
            FileSizeMb = 3.86
        },

        new MapDataset
        {
            Name = "RDP 0.020",
            FileName = "countries_rdp_0_02.geojson",
            Description = "Ramer-Douglas-Peucker",
            PointCount = 51459,
            ReductionPercent = 48.32,
            FileSizeMb = 2.14
        },

        new MapDataset
        {
            Name = "VW 0.050",
            FileName = "countries_vw_0_05.geojson",
            Description = "Visvalingam-Whyatt",
            PointCount = 53271,
            ReductionPercent = 46.50,
            FileSizeMb = 2.20
        }
        ];
    }
}