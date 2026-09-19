using WorldMap.Core.Models;

namespace WorldMap.Core.Projections;

/// <summary>
/// Belgian Lambert 2008-projectie.
/// Zet longitude/latitude om naar Belgische X/Y-coördinaten
/// en kan ook terugrekenen van X/Y naar longitude/latitude.
/// </summary>
public sealed class Lambert2008Projection : IMapProjection
{
    public string Name =>
        "Belgian Lambert 2008";

    // Waarden van de GRS80-vorm van de aarde.
    private const double A =
        6378137.0;

    private const double InverseFlattening =
        298.257222101;

    // Latitude van het startpunt van de projectie.
    private const double LatitudeOfOriginDegrees =
        50.797815;

    // Centrale lengtegraad van de projectie.
    private const double CentralMeridianDegrees =
        4.35921583333333;

    // Twee breedtegraden waarop de Lambert-projectie
    // het best aansluit bij België.
    private const double StandardParallel1Degrees =
        49.8333333333333;

    private const double StandardParallel2Degrees =
        51.1666666666667;

    // Extra verschuiving in X en Y.
    // Hierdoor krijgen de Belgische coördinaten positieve waarden.
    private const double FalseEasting =
        649328.0;

    private const double FalseNorthing =
        665262.0;

    // Waarde die aangeeft hoe sterk de aarde afwijkt
    // van een perfecte bol.
    private readonly double _eccentricity;

    // Waarden die één keer berekend worden
    // en daarna gebruikt worden bij Project en Unproject.
    private readonly double _n;
    private readonly double _f;
    private readonly double _rho0;

    public Lambert2008Projection()
    {
        // Bereken hoe sterk de aarde afgeplat is.
        double flattening =
            1.0 / InverseFlattening;

        // Bereken eccentricity op basis van flattening.
        _eccentricity =
            Math.Sqrt(
                2.0 * flattening -
                flattening * flattening);

        // Zet de belangrijke hoeken om van graden naar radialen.
        double phi0 =
            DegreesToRadians(
                LatitudeOfOriginDegrees);

        double phi1 =
            DegreesToRadians(
                StandardParallel1Degrees);

        double phi2 =
            DegreesToRadians(
                StandardParallel2Degrees);

        // Hulpwaarden voor de Lambert-formules.
        double m1 =
            M(phi1);

        double m2 =
            M(phi2);

        double t1 =
            T(phi1);

        double t2 =
            T(phi2);

        double t0 =
            T(phi0);

        // Bereken een vaste Lambert-waarde.
        _n =
            Math.Log(m1 / m2)
            /
            Math.Log(t1 / t2);

        // Bereken nog een vaste schaalwaarde.
        _f =
            m1 /
            (_n * Math.Pow(t1, _n));

        // Afstand vanaf het projectiecentrum
        // tot de latitude van oorsprong.
        _rho0 =
            A *
            _f *
            Math.Pow(t0, _n);
    }

    /// <summary>
    /// Zet longitude/latitude om naar Lambert X/Y.
    /// </summary>
    public ProjectedPoint Project(
        GeoCoordinate coordinate)
    {
        // Zet latitude en longitude om naar radialen.
        double latitude =
            DegreesToRadians(
                coordinate.Latitude);

        double longitude =
            DegreesToRadians(
                coordinate.Longitude);

        double centralMeridian =
            DegreesToRadians(
                CentralMeridianDegrees);

        // Bereken hulpwaarde voor de latitude.
        double t =
            T(latitude);

        // Bereken afstand tot het Lambert-projectiecentrum.
        double rho =
            A *
            _f *
            Math.Pow(t, _n);

        // Bereken hoek ten opzichte van de centrale meridiaan.
        double theta =
            _n *
            (longitude - centralMeridian);

        // Bereken uiteindelijke X-coördinaat.
        double x =
            FalseEasting +
            rho * Math.Sin(theta);

        // Bereken uiteindelijke Y-coördinaat.
        double y =
            FalseNorthing +
            _rho0 -
            rho * Math.Cos(theta);

        return new ProjectedPoint(
            x,
            y);
    }

    /// <summary>
    /// Zet Lambert X/Y terug om naar longitude/latitude.
    /// </summary>
    public GeoCoordinate Unproject(
        ProjectedPoint point)
    {
        // Haal de vaste X- en Y-verschuiving er weer af.
        double dx =
            point.X -
            FalseEasting;

        double dy =
            _rho0 -
            (point.Y - FalseNorthing);

        // Bereken afstand van het punt
        // tot het Lambert-projectiecentrum.
        double rho =
            Math.Sqrt(
                dx * dx +
                dy * dy);

        if (_n < 0)
        {
            rho =
                -rho;
        }

        // Bereken de hoek van het punt.
        double theta =
            Math.Atan2(
                dx,
                dy);

        // Bereken opnieuw de t-waarde.
        double t =
            Math.Pow(
                rho / (A * _f),
                1.0 / _n);

        // Zet t terug om naar latitude.
        double latitude =
            LatitudeFromT(t);

        // Bereken longitude terug.
        double longitude =
            DegreesToRadians(
                CentralMeridianDegrees)
            +
            theta / _n;

        // Zet radialen terug om naar graden.
        return new GeoCoordinate(
            RadiansToDegrees(longitude),
            RadiansToDegrees(latitude));
    }

    /// <summary>
    /// Hulpberekening die gebruikt wordt
    /// in de Lambert-formules.
    /// </summary>
    private double M(
        double latitude)
    {
        double sinLatitude =
            Math.Sin(latitude);

        return Math.Cos(latitude)
               /
               Math.Sqrt(
                   1.0 -
                   _eccentricity *
                   _eccentricity *
                   sinLatitude *
                   sinLatitude);
    }

    /// <summary>
    /// Tweede hulpberekening voor de Lambert-formules.
    /// Houdt rekening met latitude en de vorm van de aarde.
    /// </summary>
    private double T(
        double latitude)
    {
        double sinLatitude =
            Math.Sin(latitude);

        double numerator =
            Math.Tan(
                Math.PI / 4.0 -
                latitude / 2.0);

        double eccentricityTerm =
            (1.0 -
             _eccentricity *
             sinLatitude)
            /
            (1.0 +
             _eccentricity *
             sinLatitude);

        return numerator
               /
               Math.Pow(
                   eccentricityTerm,
                   _eccentricity / 2.0);
    }

    /// <summary>
    /// Rekent een t-waarde terug naar latitude.
    /// Dit gebeurt stap voor stap omdat er
    /// geen simpele directe formule voor is.
    /// </summary>
    private double LatitudeFromT(
        double t)
    {
        // Eerste schatting van de latitude.
        double latitude =
            Math.PI / 2.0 -
            2.0 * Math.Atan(t);

        // Verbeter de schatting meerdere keren.
        for (int i = 0; i < 15; i++)
        {
            double sinLatitude =
                Math.Sin(latitude);

            double eccentricityTerm =
                (1.0 -
                 _eccentricity *
                 sinLatitude)
                /
                (1.0 +
                 _eccentricity *
                 sinLatitude);

            double newLatitude =
                Math.PI / 2.0
                -
                2.0 *
                Math.Atan(
                    t *
                    Math.Pow(
                        eccentricityTerm,
                        _eccentricity / 2.0));

            // Als de nieuwe waarde bijna hetzelfde is,
            // hoeven we niet verder te rekenen.
            if (Math.Abs(
                    newLatitude -
                    latitude)
                < 1e-12)
            {
                latitude =
                    newLatitude;

                break;
            }

            latitude =
                newLatitude;
        }

        return latitude;
    }

    /// <summary>
    /// Zet graden om naar radialen.
    /// Math.Sin, Math.Cos, enz. werken met radialen.
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