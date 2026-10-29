using System.Globalization;

namespace Archive.Core.Dates;

public static class UtcInstant
{
    public const string FromDescription = "Inclusive. UTC. Format yyyy-MM-ddTHH:mm:ssZ, or yyyy-MM-dd for 00:00:00 UTC.";
    public const string ToDescription = "Inclusive. UTC. Format yyyy-MM-ddTHH:mm:ssZ, or yyyy-MM-dd for 23:59:59 UTC (the whole day).";

    private const DateTimeStyles UtcStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

    // Stored instants are whole seconds, so 23:59:59 covers the rest of the day.
    private static readonly TimeSpan EndOfDay = new(23, 59, 59);

    public static bool TryParseFrom(string? value, out DateTime utc) => TryParse(value, TimeSpan.Zero, out utc);

    public static bool TryParseTo(string? value, out DateTime utc) => TryParse(value, EndOfDay, out utc);

    public static DateTime ToWholeSeconds(DateTimeOffset instant) =>
        new(instant.UtcTicks - instant.UtcTicks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

    private static bool TryParse(string? value, TimeSpan dateOnlyTime, out DateTime utc)
    {
        if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, UtcStyles, out utc))
        {
            utc += dateOnlyTime;
            return true;
        }

        return DateTime.TryParseExact(value, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture, UtcStyles, out utc);
    }
}