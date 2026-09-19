using WorldMap.Core.Models;

namespace WorldMap.Core.Interfaces;

public interface ICountryRepository
{
    Task<IReadOnlyList<Country>> LoadCountriesAsync(
        string filePath);
}