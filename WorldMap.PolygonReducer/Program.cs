using WorldMap.Core.Interfaces;
using WorldMap.Core.Models;
using WorldMap.PolygonReducer.Reducers;

Console.WriteLine("WorldMap Polygon Reducer");
Console.WriteLine("========================");
Console.WriteLine();

string inputFile =
    Path.Combine(
        AppContext.BaseDirectory,
        "Data",
        "ne_50m_admin_0_countries.geojson");

string outputDirectory =
    Path.Combine(
        AppContext.BaseDirectory,
        "Output");

Directory.CreateDirectory(outputDirectory);

if (!File.Exists(inputFile))
{
    Console.WriteLine(
        $"Bronbestand niet gevonden:\n{inputFile}");

    return;
}

RunReduction(
    "Ramer-Douglas-Peucker",
    new RdpGeoJsonReducer(),
    0.020,
    inputFile,
    Path.Combine(
        outputDirectory,
        "countries_rdp_0_02.geojson"));

Console.WriteLine();

RunReduction(
    "Visvalingam-Whyatt",
    new VwGeoJsonReducer(),
    0.050,
    inputFile,
    Path.Combine(
        outputDirectory,
        "countries_vw_0_05.geojson"));

Console.WriteLine();
Console.WriteLine("Klaar.");

static void RunReduction(
    string displayName,
    IPolygonReducer reducer,
    double parameter,
    string inputFile,
    string outputFile)
{
    Console.WriteLine(
        $"Algoritme: {displayName}");

    Console.WriteLine(
        $"Parameter: {parameter}");

    ReductionStatistics statistics =
        reducer.Reduce(
            inputFile,
            outputFile,
            parameter);

    double reductionPercentage =
        statistics.OriginalPointCount == 0
            ? 0.0
            : 100.0 *
              (statistics.OriginalPointCount -
               statistics.ReducedPointCount)
              / statistics.OriginalPointCount;

    Console.WriteLine(
        $"Originele punten : {statistics.OriginalPointCount:N0}");

    Console.WriteLine(
        $"Nieuwe punten     : {statistics.ReducedPointCount:N0}");

    Console.WriteLine(
        $"Reductie          : {reductionPercentage:F2}%");

    Console.WriteLine(
        $"Rekentijd         : {statistics.ProcessingMilliseconds} ms");

    Console.WriteLine(
        $"Input grootte     : " +
        $"{statistics.InputFileSizeBytes / 1024.0 / 1024.0:F2} MB");

    Console.WriteLine(
        $"Output grootte    : " +
        $"{statistics.OutputFileSizeBytes / 1024.0 / 1024.0:F2} MB");

    Console.WriteLine(
        $"Output            : {outputFile}");
}