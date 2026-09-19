using WorldMap.Core.Models;

namespace WorldMap.Core.Services;

public static class CityPriorityService
{
    public static double CalculatePriority(
        City city)
    {
        double score = 0.0;

        if (city.IsCapital)
        {
            score += 1_000_000;
        }

        if (city.PlaceType == "city")
        {
            score += 100_000;
        }
        else if (city.PlaceType == "town")
        {
            score += 10_000;
        }

        if (city.Population.HasValue)
        {
            score +=
                Math.Log10(
                    Math.Max(
                        city.Population.Value,
                        1))
                * 100_000;
        }

        return score;
    }
}