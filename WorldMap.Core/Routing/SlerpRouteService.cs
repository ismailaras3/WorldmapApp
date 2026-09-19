using WorldMap.Core.Models;

namespace WorldMap.Core.Routing;

public static class SlerpRouteService
{
    public static IReadOnlyList<GeoCoordinate> GenerateRoute(
        GeoCoordinate start,
        GeoCoordinate end,
        int numberOfPoints = 100)
    {
        if (numberOfPoints < 2)
            throw new ArgumentOutOfRangeException(nameof(numberOfPoints));

        Vector3D a = ToUnitVector(start);
        Vector3D b = ToUnitVector(end);

        double dot =
            a.X * b.X +
            a.Y * b.Y +
            a.Z * b.Z;

        dot = Math.Clamp(dot, -1.0, 1.0);

        double theta = Math.Acos(dot);

        // Bij vrijwel identieke punten is gewone interpolatie voldoende.
        if (theta < 1e-12)
        {
            return [start, end];
        }

        double sinTheta = Math.Sin(theta);

        List<GeoCoordinate> points = [];

        for (int i = 0; i < numberOfPoints; i++)
        {
            double t =
                i / (double)(numberOfPoints - 1);

            double weightA =
                Math.Sin((1.0 - t) * theta)
                / sinTheta;

            double weightB =
                Math.Sin(t * theta)
                / sinTheta;

            double x =
                weightA * a.X +
                weightB * b.X;

            double y =
                weightA * a.Y +
                weightB * b.Y;

            double z =
                weightA * a.Z +
                weightB * b.Z;

            double length =
                Math.Sqrt(x * x + y * y + z * z);

            x /= length;
            y /= length;
            z /= length;

            double latitude =
                Math.Atan2(
                    z,
                    Math.Sqrt(x * x + y * y));

            double longitude =
                Math.Atan2(y, x);

            points.Add(
                new GeoCoordinate(
                    RadiansToDegrees(longitude),
                    RadiansToDegrees(latitude)));
        }

        return points;
    }

    private static Vector3D ToUnitVector(
        GeoCoordinate coordinate)
    {
        double latitude =
            DegreesToRadians(coordinate.Latitude);

        double longitude =
            DegreesToRadians(coordinate.Longitude);

        return new Vector3D(
            Math.Cos(latitude) * Math.Cos(longitude),
            Math.Cos(latitude) * Math.Sin(longitude),
            Math.Sin(latitude));
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static double RadiansToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }

    private readonly record struct Vector3D(
        double X,
        double Y,
        double Z);
}