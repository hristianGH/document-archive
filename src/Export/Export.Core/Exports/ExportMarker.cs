using System.Globalization;

namespace Export.Core.Exports;

public static class ExportMarker
{
    public static string For(DateOnly exportDate) =>
        string.Create(CultureInfo.InvariantCulture, $"Exported on {exportDate:yyyy-MM-dd}");
}