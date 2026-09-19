using System.Diagnostics;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Simplify;
using WorldMap.Core.Interfaces;
using WorldMap.Core.Models;

namespace WorldMap.PolygonReducer.Reducers;

public sealed class RdpGeoJsonReducer : IPolygonReducer
{
    public ReductionAlgorithm Algorithm =>
        ReductionAlgorithm.RamerDouglasPeucker;

    public ReductionStatistics Reduce(
        string inputFile,
        string outputFile,
        double parameter)
    {
        if (!File.Exists(inputFile))
        {
            throw new FileNotFoundException(
                "Input GeoJSON-bestand bestaat niet.",
                inputFile);
        }

        string json =
            File.ReadAllText(inputFile);

        var reader =
            new GeoJsonReader();

        FeatureCollection? featureCollection =
            reader.Read<FeatureCollection>(json);

        if (featureCollection is null)
        {
            throw new InvalidOperationException(
                "GeoJSON kon niet worden gelezen.");
        }

        int originalPointCount =
            CountPoints(featureCollection);

        long inputFileSize =
            new FileInfo(inputFile).Length;

        Stopwatch stopwatch =
            Stopwatch.StartNew();

        var outputFeatures =
            new FeatureCollection();

        foreach (IFeature feature in featureCollection)
        {
            Geometry geometry =
                feature.Geometry;

            Geometry simplified =
                DouglasPeuckerSimplifier.Simplify(
                    geometry,
                    parameter);

            var attributes =
                new AttributesTable();

            foreach (string name in feature.Attributes.GetNames())
            {
                attributes.Add(
                    name,
                    feature.Attributes[name]);
            }

            outputFeatures.Add(
                new Feature(
                    simplified,
                    attributes));
        }

        stopwatch.Stop();

        int reducedPointCount =
            CountPoints(outputFeatures);

        var writer =
            new GeoJsonWriter();

        string outputJson =
            writer.Write(outputFeatures);

        File.WriteAllText(
            outputFile,
            outputJson);

        long outputFileSize =
            new FileInfo(outputFile).Length;

        return new ReductionStatistics
        {
            Algorithm =
                Algorithm.ToString(),

            Parameter =
                parameter,

            OriginalPointCount =
                originalPointCount,

            ReducedPointCount =
                reducedPointCount,

            ProcessingMilliseconds =
                stopwatch.ElapsedMilliseconds,

            InputFileSizeBytes =
                inputFileSize,

            OutputFileSizeBytes =
                outputFileSize
        };
    }

    private static int CountPoints(
        FeatureCollection featureCollection)
    {
        int count = 0;

        foreach (IFeature feature in featureCollection)
        {
            count +=
                feature.Geometry.NumPoints;
        }

        return count;
    }
}