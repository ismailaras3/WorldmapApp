using WorldMap.Core.Models;

namespace WorldMap.Core.Interfaces;

public interface IPolygonReducer
{
    ReductionAlgorithm Algorithm { get; }

    ReductionStatistics Reduce(
        string inputFile,
        string outputFile,
        double parameter);
}