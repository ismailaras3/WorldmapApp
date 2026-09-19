using System.Diagnostics;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Newtonsoft.Json;
using WorldMap.Core.Interfaces;
using WorldMap.Core.Models;
using System.Diagnostics;

namespace WorldMap.Infrastructure.GeoJson;

public sealed class GeoJsonCountryRepository
    : ICountryRepository
{
    public async Task<IReadOnlyList<Country>>
        LoadCountriesAsync(
            string filePath)
    {
        string json =
            await File.ReadAllTextAsync(//hier lees ik de goejson bestand in 
                filePath);

        FeatureCollection? featureCollection =
            JsonConvert.DeserializeObject<
                FeatureCollection>(json);

        if (featureCollection is null)
        {
            throw new InvalidOperationException(
                "GeoJSON kon niet worden ingelezen.");
        }

        List<Country> countries = [];

        foreach (Feature feature
                 in featureCollection.Features) //elke feature reprecenteerd een land 
        {
            string countryName =
                GetCountryName(feature);
            if (countryName == GetCountryName(
        featureCollection.Features.First()))
            {
                Debug.WriteLine(
                    "GEOJSON KEYS: " +
                    string.Join(
                        ", ",
                        feature.Properties.Keys));
            }

            string continent =
                GetContinent(feature);

            List<GeoPolygon> polygons = [];

            switch (feature.Geometry)
            {
                case Polygon polygon:
                    polygons.Add(
                        ConvertPolygon(//als het land uit 1 deel bestaat 1 polygon
                            polygon));
                    break;

                case MultiPolygon multiPolygon:// als het land uit meerdere eilanden bestaan 

                    foreach (Polygon polygonPart
                             in multiPolygon.Coordinates)
                    {
                        polygons.Add(
                            ConvertPolygon(
                                polygonPart));
                    }

                    break;
            }

            if (polygons.Count > 0)
            {
                countries.Add(
                    new Country(
                        countryName,
                        polygons,
                        continent));
            }
        }

        return countries;
    }

    private static string GetCountryName(
        Feature feature)
    {
        string[] possibleKeys =
        [
            "ADMIN",
            "NAME",
            "NAME_EN",
            "SOVEREIGNT"
        ];

        foreach (string key
                 in possibleKeys)
        {
            if (feature.Properties.TryGetValue(
                    key,
                    out object? value) &&
                value is not null)
            {
                return value.ToString()
                       ?? "Unknown";
            }
        }

        return "Unknown";
    }

    private static string GetContinent(
    Feature feature)
    {
        foreach (KeyValuePair<string, object> property
                 in feature.Properties)
        {
            if (string.Equals(
                    property.Key,
                    "CONTINENT",
                    StringComparison.OrdinalIgnoreCase))
            {
                return property.Value?.ToString()
                       ?? "Unknown";
            }
        }

        return "Unknown";
    }

    private static GeoPolygon ConvertPolygon(
        Polygon polygon)
    {
        List<IReadOnlyList<GeoCoordinate>>
            rings = [];

        foreach (LineString ring
                 in polygon.Coordinates)
        {
            List<GeoCoordinate>
                coordinates = [];

            foreach (IPosition position
                     in ring.Coordinates)
            {
                coordinates.Add(
                    new GeoCoordinate(
                        position.Longitude,
                        position.Latitude));
            }

            rings.Add(
                coordinates);
        }

        if (rings.Count == 0)//buitenste rand van een land 
        {
            return new GeoPolygon(
                []);
        }

        IReadOnlyList<GeoCoordinate>
            outerRing =
                rings[0];

        IReadOnlyList<
            IReadOnlyList<GeoCoordinate>>
            holes =
                rings
                    .Skip(1)
                    .ToList();

        return new GeoPolygon(
            outerRing,
            holes);
    }
}