using WorldMap.Core.Models;

namespace WorldMap.Core.Geometry;

public static class GeoPointInPolygon
{
    public static bool Contains(
        GeoPolygon polygon,
        GeoCoordinate point)
    {
        // Eerst controleren of het punt
        // binnen de buitenrand van het land ligt.
        if (!IsInsideRing(
                polygon.OuterRing,
                point))
        {
            return false;
        }

        // Een punt dat in een gat ligt,
        // telt niet als land.
        foreach (IReadOnlyList<GeoCoordinate> hole
                 in polygon.Holes)
        {
            if (IsInsideRing(
                    hole,
                    point))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsInsideRing(
        IReadOnlyList<GeoCoordinate> ring,
        GeoCoordinate point)
    {
        if (ring.Count < 3)
            return false;

        /*
         * Sommige polygonen kruisen de kaartnaad
         * tussen -180° en +180°.
         *
         * Bijvoorbeeld:
         * 179° en -179° liggen geografisch vlak naast elkaar.
         *
         * Daarom verschuiven we bij zulke polygonen
         * negatieve longitudes tijdelijk met +360°.
         */
        double minLongitude =
            ring.Min(p => p.Longitude);

        double maxLongitude =
            ring.Max(p => p.Longitude);

        bool crossesDateLine =
            maxLongitude - minLongitude > 180.0;

        double pointX =
            AdjustLongitude(
                point.Longitude,
                crossesDateLine);

        double pointY =
            point.Latitude;

        bool inside =
            false;

        int previousIndex =
            ring.Count - 1;

        for (int currentIndex = 0;
             currentIndex < ring.Count;
             currentIndex++)
        {
            GeoCoordinate current =
                ring[currentIndex];

            GeoCoordinate previous =
                ring[previousIndex];

            double currentX =
                AdjustLongitude(
                    current.Longitude,
                    crossesDateLine);

            double previousX =
                AdjustLongitude(
                    previous.Longitude,
                    crossesDateLine);

            double currentY =
                current.Latitude;

            double previousY =
                previous.Latitude;

            // Controleer of een horizontale lijn
            // vanaf het punt deze polygonrand kruist.
            bool crossesLatitude =
                (currentY > pointY) !=
                (previousY > pointY);

            if (crossesLatitude)
            {
                double intersectionX =
                    previousX +
                    (
                        pointY - previousY
                    ) *
                    (
                        currentX - previousX
                    ) /
                    (
                        currentY - previousY
                    );

                if (pointX < intersectionX)
                {
                    inside =
                        !inside;
                }
            }

            previousIndex =
                currentIndex;
        }

        return inside;
    }

    private static double AdjustLongitude(
        double longitude,
        bool crossesDateLine)
    {
        if (crossesDateLine &&
            longitude < 0.0)
        {
            return longitude + 360.0;
        }

        return longitude;
    }
}