using WorldMap.Core.Models;

namespace WorldMap.Core.Interfaces;

public interface ICityRepository
{
    Task<IReadOnlyList<City>> LoadCitiesAsync(
        string filePath);
}