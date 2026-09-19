using WorldMap.Core.Models;

namespace WorldMap.Core.Projections;

/// <summary>
/// Azimuthal Equidistant-projectie.
///
/// Deze projectie zet geografische coördinaten (longitude/latitude)
/// om naar vlakke X/Y-coördinaten.
///
/// Het projectiecentrum ligt standaard in Brussel.
/// </summary>
public sealed class AzimuthalEquidistantProjection : IMapProjection
{
    public string Name =>
        "Azimuthal Equidistant";

    // Centrum van de projectie.
    // We kiezen Brussel zodat Europa centraal staat.
    public double CenterLongitude { get; }

    public double CenterLatitude { get; }

    // Zelfde centrum, maar intern opgeslagen in radialen.
    // Trigonometrische functies in C# werken met radialen.
    private readonly double _lambda0;
    private readonly double _phi0;

    public AzimuthalEquidistantProjection(
        double centerLongitude = 4.3517,
        double centerLatitude = 50.8503)
    {
        CenterLongitude =
            centerLongitude;

        CenterLatitude =
            centerLatitude;

        // Longitude van het projectiecentrum naar radialen.
        _lambda0 =
            DegreesToRadians(
                centerLongitude);

        // Latitude van het projectiecentrum naar radialen.
        _phi0 =
            DegreesToRadians(
                centerLatitude);
    }

    /// <summary>
    /// Zet longitude/latitude om naar geprojecteerde X/Y-coördinaten.
    /// </summary>
    public ProjectedPoint Project(
        GeoCoordinate coordinate)
    {
        // Longitude en latitude omzetten naar radialen.
        double lambda =
            DegreesToRadians(
                coordinate.Longitude);

        double phi =
            DegreesToRadians(
                coordinate.Latitude);

        // Verschil in longitude tussen het punt
        // en het projectiecentrum.
        double deltaLambda =
            NormalizeLongitudeRadians(
                lambda - _lambda0);

        // Trigonometrische waarden van het centrum.
        double sinPhi0 =
            Math.Sin(_phi0);

        double cosPhi0 =
            Math.Cos(_phi0);

        // Trigonometrische waarden van het te projecteren punt.
        double sinPhi =
            Math.Sin(phi);

        double cosPhi =
            Math.Cos(phi);

        // Cosinus van de centrale hoek tussen:
        // - het projectiecentrum
        // - het geografische punt
        //
        // Dit komt uit de spherical law of cosines.
        double cosC =
            sinPhi0 * sinPhi +
            cosPhi0 *
            cosPhi *
            Math.Cos(deltaLambda);

        // Door floating-point afronding kan cosC bv.
        // 1.0000000001 worden.
        // Acos accepteert alleen waarden tussen -1 en 1.
        cosC =
            Math.Clamp(
                cosC,
                -1.0,
                1.0);

        // Centrale hoek tussen het centrum en het punt.
        double c =
            Math.Acos(cosC);

        // Als c bijna nul is, ligt het punt exact
        // op het projectiecentrum.
        if (Math.Abs(c) < 1e-12)
        {
            return new ProjectedPoint(
                0,
                0);
        }

        double sinC =
            Math.Sin(c);

        // Het antipodale punt ligt exact tegenover
        // het projectiecentrum op de aardbol.
        //
        // De projectie is daar singulier:
        // sin(c) wordt nul en k = c / sin(c)
        // kan niet betrouwbaar berekend worden.
        if (Math.Abs(sinC) < 1e-12)
        {
            return new ProjectedPoint(
                double.NaN,
                double.NaN);
        }

        // Schaalfactor van de Azimuthal Equidistant-formule.
        double k =
            c / sinC;

        // Projectieformule voor X.
        double x =
            k *
            cosPhi *
            Math.Sin(deltaLambda);

        // Projectieformule voor Y.
        double y =
            k *
            (
                cosPhi0 * sinPhi -
                sinPhi0 *
                cosPhi *
                Math.Cos(deltaLambda)
            );

        return new ProjectedPoint(
            x,
            y);
    }

    /// <summary>
    /// Inverse projectie:
    /// zet geprojecteerde X/Y-coördinaten
    /// terug om naar longitude/latitude.
    /// </summary>
    public GeoCoordinate Unproject(
        ProjectedPoint point)
    {
        double x =
            point.X;

        double y =
            point.Y;

        // Afstand van het geprojecteerde punt
        // tot het centrum van de kaart.
        double rho =
            Math.Sqrt(
                x * x +
                y * y);

        // Als rho bijna nul is,
        // zitten we exact op het projectiecentrum.
        if (rho < 1e-12)
        {
            return new GeoCoordinate(
                CenterLongitude,
                CenterLatitude);
        }

        // Voor deze genormaliseerde sferische projectie
        // is de centrale hoek c gelijk aan rho.
        double c =
            rho;

        double sinC =
            Math.Sin(c);

        double cosC =
            Math.Cos(c);

        double sinPhi0 =
            Math.Sin(_phi0);

        double cosPhi0 =
            Math.Cos(_phi0);

        // Inverse formule voor latitude.
        double latitude =
            Math.Asin(
                cosC * sinPhi0 +
                (
                    y *
                    sinC *
                    cosPhi0
                ) / rho);

        // Inverse formule voor longitude.
        double longitude =
            _lambda0 +
            Math.Atan2(
                x * sinC,
                rho * cosPhi0 * cosC -
                y * sinPhi0 * sinC);

        // Longitude opnieuw normaliseren naar
        // het bereik -π tot +π.
        longitude =
            NormalizeLongitudeRadians(
                longitude);

        // Intern werkten we in radialen.
        // Voor de rest van de applicatie geven
        // we opnieuw graden terug.
        return new GeoCoordinate(
            RadiansToDegrees(longitude),
            RadiansToDegrees(latitude));
    }

    /// <summary>
    /// Normaliseert longitude in radialen
    /// naar het bereik -π tot +π.
    ///
    /// Dit komt overeen met -180° tot +180°.
    /// </summary>
    private static double NormalizeLongitudeRadians(
        double longitude)
    {
        while (longitude > Math.PI)
        {
            longitude -=
                2.0 * Math.PI;
        }

        while (longitude < -Math.PI)
        {
            longitude +=
                2.0 * Math.PI;
        }

        return longitude;
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