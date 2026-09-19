namespace WorldMap.Core.Models;

public sealed class ReductionStatistics
{
    public string Algorithm { get; init; } = "";

    public double Parameter { get; init; }

    public int OriginalPointCount { get; init; }

    public int ReducedPointCount { get; init; }

    public double ReductionPercentage =>
        OriginalPointCount == 0
            ? 0
            : 100.0 *
              (OriginalPointCount - ReducedPointCount)
              / OriginalPointCount;

    public long ProcessingMilliseconds { get; init; }

    public long InputFileSizeBytes { get; init; }

    public long OutputFileSizeBytes { get; init; }
}