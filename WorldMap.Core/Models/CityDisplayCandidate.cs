namespace WorldMap.Core.Models;

public sealed class CityDisplayCandidate
{
    public City City { get; init; } = null!;

    public double Priority { get; init; }
}