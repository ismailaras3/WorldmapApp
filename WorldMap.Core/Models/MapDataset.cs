namespace WorldMap.Core.Models;

public sealed class MapDataset
{
    public string Name { get; init; } = "";

    public string FileName { get; init; } = "";

    public string Description { get; init; } = "";

    public int? PointCount { get; init; }

    public double? ReductionPercent { get; init; }

    public double? FileSizeMb { get; init; }
}