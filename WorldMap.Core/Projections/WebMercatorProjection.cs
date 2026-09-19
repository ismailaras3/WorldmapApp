using WorldMap.Core.Models;

namespace WorldMap.Core.Projections;

/// <summary>
/// Web Mercator-projectie.
/// Zet longitude/latitude om naar X/Y-coördinaten
/// en kan ook terugrekenen van X/Y naar longitude/latitude.
/// </summary>
public sealed class WebMercatorProjection : IMapProjection
{
    // Web Mercator kan de polen niet correct tonen.
    // Daarom beperken we latitude tot ongeveer ±85 graden.
    public const double MaxLatitude = 85.05112878;

    public string Name =>
        "Web Mercator";

    /// <summary>
    /// Zet longitude/latitude om naar Web Mercator X/Y.
    /// </summary>
    public ProjectedPoint Project(
        GeoCoordinate coordinate)
    {
        double longitude =
            coordinate.Longitude;

        // Beperk latitude zodat de Mercator-formule
        // niet naar oneindig gaat aan de polen.
        double latitude =
            Math.Clamp(
                coordinate.Latitude,
                -MaxLatitude,
                MaxLatitude);

        // Longitude wordt voor X gewoon omgezet naar radialen.
        double x =
            DegreesToRadians(
                longitude);

        // Latitude moet ook eerst naar radialen.
        double latitudeRadians =
            DegreesToRadians(
                latitude);

        // Web Mercator-formule voor de Y-coördinaat.
        // Hoe dichter bij de polen, hoe sterker de kaart uitrekt.
        double y =
            Math.Log(
                Math.Tan(
                    Math.PI / 4.0 +
                    latitudeRadians / 2.0));

        return new ProjectedPoint(
            x,
            y);
    }

    /// <summary>
    /// Zet Web Mercator X/Y terug om
    /// naar longitude/latitude.
    /// </summary>
    public GeoCoordinate Unproject(
        ProjectedPoint point)
    {
        // X terug omzetten van radialen naar graden.
        double longitude =
            RadiansToDegrees(
                point.X);

        // Inverse Web Mercator-formule om
        // de oorspronkelijke latitude terug te vinden.
        double latitude =
            RadiansToDegrees(
                2.0 *
                Math.Atan(
                    Math.Exp(point.Y))
                - Math.PI / 2.0);

        return new GeoCoordinate(
            longitude,
            latitude);
    }

    /// <summary>
    /// Zet graden om naar radialen.
    /// </summary>
    private static double DegreesToRadians(
        double degrees)
    {
        return degrees *
               Math.PI /
               180.0;
    }

    /// <summary>
    /// Zet radialen terug om naar graden.
    /// </summary>
    private static double RadiansToDegrees(
        double radians)
    {
        return radians *
               180.0 /
               Math.PI;
    }
}