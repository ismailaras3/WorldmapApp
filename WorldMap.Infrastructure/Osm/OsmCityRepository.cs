using OsmSharp;
using OsmSharp.Streams;
using WorldMap.Core.Interfaces;
using WorldMap.Core.Models;

namespace WorldMap.Infrastructure.Osm;

public sealed class OsmCityRepository : ICityRepository
{
    public Task<IReadOnlyList<City>> LoadCitiesAsync(
        string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "OSM-bestand niet gevonden.",
                filePath);
        }

        IReadOnlyList<City> cities =
            LoadCities(filePath);

        return Task.FromResult(cities);
    }

    private static IReadOnlyList<City> LoadCities(
        string filePath)
    {
        List<City> cities = [];

        using FileStream stream =
            File.OpenRead(filePath);

        var source =
            new XmlOsmStreamSource(stream);

        foreach (OsmGeo osmGeo in source)
        {
            if (osmGeo is not Node node)
                continue;

            if (!node.Latitude.HasValue ||
                !node.Longitude.HasValue)
            {
                continue;
            }

            if (node.Tags is null)
                continue;

            if (!node.Tags.TryGetValue(
                    "place",
                    out string? placeType))
            {
                continue;
            }

            if (placeType != "city" &&
                placeType != "town")
            {
                continue;
            }

            if (!node.Tags.TryGetValue(
                    "name",
                    out string? name) ||
                string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            long? population =
                ParsePopulation(node);

            bool isCapital =
                IsCapital(node);

            City city =
                new City
                {
                    OsmId =
                        node.Id ?? 0,

                    Name =
                        name,

                    Longitude =
                        node.Longitude.Value,

                    Latitude =
                        node.Latitude.Value,

                    PlaceType =
                        placeType,

                    Population =
                        population,

                    IsCapital =
                        isCapital
                };

            cities.Add(city);
        }

        return cities;
    }

    private static long? ParsePopulation(
        Node node)
    {
        if (node.Tags is null)
            return null;

        if (!node.Tags.TryGetValue(
                "population",
                out string? value))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
            return null;

        string cleaned =
            value
                .Replace(",", "")
                .Replace(".", "")
                .Replace(" ", "");

        if (long.TryParse(
                cleaned,
                out long population))
        {
            return population;
        }

        return null;
    }

    private static bool IsCapital(
        Node node)
    {
        if (node.Tags is null)
            return false;

        if (!node.Tags.TryGetValue(
                "capital",
                out string? capital))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(capital))
            return false;

        return
            capital.Equals(
                "yes",
                StringComparison.OrdinalIgnoreCase)
            ||
            capital == "2"
            ||
            capital == "3"
            ||
            capital == "4";
    }
}