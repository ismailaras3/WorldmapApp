using GeoTimeZone;
using TimeZoneConverter;
using WorldMap.Core.Models;

namespace WorldMap.Infrastructure.TimeZones;

public sealed class TimeZoneService
{
    public TimeZoneInfo GetTimeZone(
        GeoCoordinate coordinate)
    {
        // GeoTimeZone verwacht latitude, longitude.
        string timeZoneId =
            TimeZoneLookup
                .GetTimeZone(
                    coordinate.Latitude,
                    coordinate.Longitude)
                .Result;

        // Werkt zowel met IANA- als Windows-timezone-ID's.
        return TZConvert.GetTimeZoneInfo(
            timeZoneId);
    }

    public DateTime ConvertUtcToLocal(
        DateTime utcTime,
        GeoCoordinate coordinate)
    {
        TimeZoneInfo timeZone =
            GetTimeZone(
                coordinate);

        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(
                utcTime,
                DateTimeKind.Utc),
            timeZone);
    }
}