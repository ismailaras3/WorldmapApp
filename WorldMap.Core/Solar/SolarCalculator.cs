using WorldMap.Core.Models;

namespace WorldMap.Core.Solar;

public static class SolarCalculator
{
    /// <summary>
    /// Bereken de daglengte in uren voor een bepaalde
    /// locatie en datum.
    /// </summary>
    public static double CalculateDayLengthHours(
        GeoCoordinate coordinate,
        DateTime date)
    {
        double latitudeRadians =
            DegreesToRadians(
                coordinate.Latitude);

        // Dagnummer binnen het jaar:
        // 1 januari = 1, 31 december = 365/366.
        int dayOfYear =
            date.DayOfYear;

        // Benadering van de positie van de zon
        // doorheen het jaar.
        double gamma =
            2.0 *
            Math.PI /
            365.0 *
            (dayOfYear - 1);

        // Bereken hoe ver de zon ten noorden of zuiden
        // van de evenaar staat.
        double solarDeclination =
            0.006918
            - 0.399912 * Math.Cos(gamma)
            + 0.070257 * Math.Sin(gamma)
            - 0.006758 * Math.Cos(2.0 * gamma)
            + 0.000907 * Math.Sin(2.0 * gamma)
            - 0.002697 * Math.Cos(3.0 * gamma)
            + 0.00148 * Math.Sin(3.0 * gamma);

        // -0.833° houdt rekening met het feit dat
        // zonsopgang/zonsondergang niet exact bij 0° gebeurt.
        double solarAltitude =
            DegreesToRadians(-0.833);

        double cosHourAngle =
            (
                Math.Sin(solarAltitude)
                -
                Math.Sin(latitudeRadians) *
                Math.Sin(solarDeclination)
            )
            /
            (
                Math.Cos(latitudeRadians) *
                Math.Cos(solarDeclination)
            );

        // Zon komt niet op:
        // 24 uur donker.
        if (cosHourAngle >= 1.0)
        {
            return 0.0;
        }

        // Zon gaat niet onder:
        // 24 uur daglicht.
        if (cosHourAngle <= -1.0)
        {
            return 24.0;
        }

        double hourAngle =
            Math.Acos(
                cosHourAngle);

        // De zon beweegt gemiddeld 15 graden per uur.
        // De hoek geeft de tijd van solar noon tot sunset.
        double hourAngleDegrees =
            RadiansToDegrees(
                hourAngle);

        return
            2.0 *
            hourAngleDegrees /
            15.0;
    }

    private static double DegreesToRadians(
        double degrees)
    {
        return
            degrees *
            Math.PI /
            180.0;
    }

    private static double RadiansToDegrees(
        double radians)
    {
        return
            radians *
            180.0 /
            Math.PI;
    }
    public static string FormatHours(
    double hours)
    {
        int wholeHours =
            (int)Math.Floor(hours);

        int minutes =
            (int)Math.Round(
                (hours - wholeHours) * 60.0);

        if (minutes == 60)
        {
            wholeHours++;
            minutes = 0;
        }

        return $"{wholeHours}u {minutes:D2}m";
    }
    public static double CalculateEquationOfTimeMinutes(
    DateTime date)
    {
        int dayOfYear =
            date.DayOfYear;

        double gamma =
            2.0 *
            Math.PI /
            365.0 *
            (dayOfYear - 1);

        return
            229.18 *
            (
                0.000075
                + 0.001868 * Math.Cos(gamma)
                - 0.032077 * Math.Sin(gamma)
                - 0.014615 * Math.Cos(2.0 * gamma)
                - 0.040849 * Math.Sin(2.0 * gamma)
            );
    }

    private static double CalculateSolarDeclination(
    DateTime date)
    {
        int dayOfYear =
            date.DayOfYear;

        double gamma =
            2.0 *
            Math.PI /
            365.0 *
            (dayOfYear - 1);

        return
            0.006918
            - 0.399912 * Math.Cos(gamma)
            + 0.070257 * Math.Sin(gamma)
            - 0.006758 * Math.Cos(2.0 * gamma)
            + 0.000907 * Math.Sin(2.0 * gamma)
            - 0.002697 * Math.Cos(3.0 * gamma)
            + 0.00148 * Math.Sin(3.0 * gamma);
    }

    public static DateTime CalculateSolarNoonUtc(
    GeoCoordinate coordinate,
    DateTime date)
    {
        double equationOfTime =
            CalculateEquationOfTimeMinutes(
                date);

        /*
         * Solar noon in UTC-minuten.
         *
         * Positieve longitude = oost.
         * 4 minuten per longitudegraad.
         */
        double minutesUtc =
            720.0
            - 4.0 * coordinate.Longitude
            - equationOfTime;

        DateTime utcMidnight =
            DateTime.SpecifyKind(
                date.Date,
                DateTimeKind.Utc);

        return utcMidnight.AddMinutes(
            minutesUtc);
    }

    private static double? CalculateSunriseHourAngleDegrees(
    GeoCoordinate coordinate,
    DateTime date)
    {
        double latitude =
            DegreesToRadians(
                coordinate.Latitude);

        double solarDeclination =
            CalculateSolarDeclination(
                date);

        // Standaardwaarde voor sunrise/sunset.
        double solarAltitude =
            DegreesToRadians(
                -0.833);

        double cosHourAngle =
            (
                Math.Sin(solarAltitude)
                -
                Math.Sin(latitude) *
                Math.Sin(solarDeclination)
            )
            /
            (
                Math.Cos(latitude) *
                Math.Cos(solarDeclination)
            );

        // Geen normale sunrise/sunset:
        // pooldag of poolnacht.
        if (cosHourAngle < -1.0 ||
            cosHourAngle > 1.0)
        {
            return null;
        }

        return RadiansToDegrees(
            Math.Acos(
                cosHourAngle));
    }

    public static DateTime? CalculateSunriseUtc(
    GeoCoordinate coordinate,
    DateTime date)
    {
        double? hourAngle =
            CalculateSunriseHourAngleDegrees(
                coordinate,
                date);

        if (!hourAngle.HasValue)
            return null;

        DateTime solarNoon =
            CalculateSolarNoonUtc(
                coordinate,
                date);

        // 15 graden per uur = 4 minuten per graad.
        double minutesBeforeNoon =
            hourAngle.Value * 4.0;

        return solarNoon.AddMinutes(
            -minutesBeforeNoon);
    }

    public static DateTime? CalculateSunsetUtc(
    GeoCoordinate coordinate,
    DateTime date)
    {
        double? hourAngle =
            CalculateSunriseHourAngleDegrees(
                coordinate,
                date);

        if (!hourAngle.HasValue)
            return null;

        DateTime solarNoon =
            CalculateSolarNoonUtc(
                coordinate,
                date);

        double minutesAfterNoon =
            hourAngle.Value * 4.0;

        return solarNoon.AddMinutes(
            minutesAfterNoon);
    }

    public static string FormatClockTime(
    DateTime time)
    {
        return time.ToString("HH:mm");
    }
    public static string FormatMinutesAsClock(
    double minutes)
    {
        // Zorg dat de tijd altijd binnen één dag valt.
        minutes =
            ((minutes % 1440.0) + 1440.0)
            % 1440.0;

        int totalMinutes =
            (int)Math.Round(minutes);

        if (totalMinutes >= 1440)
            totalMinutes = 0;

        int hours =
            totalMinutes / 60;

        int remainingMinutes =
            totalMinutes % 60;

        return $"{hours:D2}:{remainingMinutes:D2}";
    }
    public static double CalculateSolarAltitudeDegrees(
    GeoCoordinate coordinate,
    DateTime utcDateTime)
    {
        int dayOfYear =
            utcDateTime.DayOfYear;

        double hour =
            utcDateTime.Hour +
            utcDateTime.Minute / 60.0 +
            utcDateTime.Second / 3600.0;

        double gamma =
            2.0 * Math.PI / 365.0 *
            (dayOfYear - 1 + (hour - 12.0) / 24.0);

        double equationOfTime =
            229.18 *
            (
                0.000075
                + 0.001868 * Math.Cos(gamma)
                - 0.032077 * Math.Sin(gamma)
                - 0.014615 * Math.Cos(2.0 * gamma)
                - 0.040849 * Math.Sin(2.0 * gamma)
            );

        double declination =
            0.006918
            - 0.399912 * Math.Cos(gamma)
            + 0.070257 * Math.Sin(gamma)
            - 0.006758 * Math.Cos(2.0 * gamma)
            + 0.000907 * Math.Sin(2.0 * gamma)
            - 0.002697 * Math.Cos(3.0 * gamma)
            + 0.00148 * Math.Sin(3.0 * gamma);

        double minutesUtc =
            hour * 60.0;

        double trueSolarTime =
            minutesUtc
            + equationOfTime
            + 4.0 * coordinate.Longitude;

        trueSolarTime =
            ((trueSolarTime % 1440.0) + 1440.0)
            % 1440.0;

        double hourAngleDegrees =
            trueSolarTime / 4.0 - 180.0;

        double latitudeRadians =
            DegreesToRadians(
                coordinate.Latitude);

        double hourAngleRadians =
            DegreesToRadians(
                hourAngleDegrees);

        double sinAltitude =
            Math.Sin(latitudeRadians) *
            Math.Sin(declination)
            +
            Math.Cos(latitudeRadians) *
            Math.Cos(declination) *
            Math.Cos(hourAngleRadians);

        sinAltitude =
            Math.Clamp(
                sinAltitude,
                -1.0,
                1.0);

        double altitudeRadians =
            Math.Asin(
                sinAltitude);

        return RadiansToDegrees(
            altitudeRadians);
    }

}