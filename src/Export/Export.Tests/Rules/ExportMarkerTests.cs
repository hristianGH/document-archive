using System.Globalization;
using Export.Core.Exports;

namespace Export.Tests.Rules;

public class ExportMarkerTests
{
    [Fact]
    [Trait("Rule", "X6")]
    public void The_marker_holds_the_export_date_as_year_month_day()
    {
        Assert.Equal("Exported on 2026-10-28", ExportMarker.For(new DateOnly(2026, 10, 28)));
    }

    [Fact]
    [Trait("Rule", "X6")]
    public void The_marker_does_not_follow_the_server_culture()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            // Thai culture uses the Buddhist calendar, where 2026 is 2569.
            CultureInfo.CurrentCulture = new CultureInfo("th-TH");
            Assert.Equal("Exported on 2026-10-28", ExportMarker.For(new DateOnly(2026, 10, 28)));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}