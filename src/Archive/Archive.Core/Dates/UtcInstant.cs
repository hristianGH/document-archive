using System.Globalization;

namespace Archive.Core.Dates;

public static class UtcInstant
{
    public const string Description = "UTC. Format yyyy-MM-ddTHH:mm:ssZ, or yyyy-MM-dd for 00:00:00 UTC.";

    private static readonly string[] Formats =
    [
        "yyyy-MM-dd",
        "yyyy-MM-dd'T'HH:mm:ss'Z'"
    ];

    public static bool TryParse(string? value, out DateTime utc) =>
        DateTime.TryParseExact(value, Formats, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out utc);
}
