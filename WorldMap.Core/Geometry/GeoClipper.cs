using WorldMap.Core.Models;

namespace WorldMap.Core.Geometry;

public static class GeoClipper
{//De klasse zorgt ervoor dat geografische polygonen alleen binnen een geldig latitudegebied blijven
    public static IReadOnlyList<GeoCoordinate> ClipLatitude(//Dit verwijdert alles onder de minimumlatitude.
        IReadOnlyList<GeoCoordinate> coordinates,
        double minLatitude,
        double maxLatitude)
    {
        if (coordinates.Count < 3)
            return Array.Empty<GeoCoordinate>();

        List<GeoCoordinate> result =
            coordinates.ToList();

        result = ClipAgainstLatitude(
            result,
            minLatitude,
            keepAbove: true);

        result = ClipAgainstLatitude(
            result,
            maxLatitude,
            keepAbove: false);

        return result;
    }

    private static List<GeoCoordinate> ClipAgainstLatitude(//Dit verwijdert alles boven de maximumlatitude.
        List<GeoCoordinate> input,
        double boundaryLatitude,
        bool keepAbove)
    {
        List<GeoCoordinate> output = [];

        if (input.Count == 0)
            return output;

        GeoCoordinate previous = input[^1];

        bool previousInside =
            IsInside(
                previous,
                boundaryLatitude,
                keepAbove);

        foreach (GeoCoordinate current in input)
        {
            bool currentInside =
                IsInside(
                    current,
                    boundaryLatitude,
                    keepAbove);

            if (currentInside)
            {
                if (!previousInside)
                {
                    output.Add(
                        FindIntersection(
                            previous,
                            current,
                            boundaryLatitude));
                }

                output.Add(current);
            }
            else if (previousInside)
            {
                output.Add(
                    FindIntersection(
                        previous,
                        current,
                        boundaryLatitude));
            }

            previous = current;
            previousInside = currentInside;
        }

        return output;
    }

    private static bool IsInside(
        GeoCoordinate coordinate,
        double boundaryLatitude,
        bool keepAbove)
    {
        return keepAbove
            ? coordinate.Latitude >= boundaryLatitude
            : coordinate.Latitude <= boundaryLatitude;
    }

    private static GeoCoordinate FindIntersection(
        GeoCoordinate start,
        GeoCoordinate end,
        double boundaryLatitude)
    {
        double latitudeDifference =
            end.Latitude - start.Latitude;

        if (Math.Abs(latitudeDifference) < 1e-12)
        {
            return new GeoCoordinate(
                start.Longitude,
                boundaryLatitude);
        }

        double t =
            (boundaryLatitude - start.Latitude)
            / latitudeDifference;

        double longitude =
            start.Longitude +
            t * (end.Longitude - start.Longitude);

        return new GeoCoordinate(
            longitude,
            boundaryLatitude);
    }

    //GeoClipper knipt polygonen af op een geldig latitudegebied voordat ik ze projecteer.
    //Dat is bijvoorbeeld nodig voor Web Mercator, omdat de projectie naar oneindig gaat aan de polen
}