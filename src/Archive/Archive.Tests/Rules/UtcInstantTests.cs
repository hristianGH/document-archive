using System.Globalization;
using Archive.Core.Dates;

namespace Archive.Tests.Rules;

public class UtcInstantTests
{
    [Theory]
    [Trait("Rule", "R14")]
    [InlineData("2026-10-28", "2026-10-28T00:00:00", "2026-10-28T23:59:59")]
    [InlineData("2026-10-28T23:15:00Z", "2026-10-28T23:15:00", "2026-10-28T23:15:00")]
    public void Utc_instants_are_read_as_utc_and_a_date_covers_the_whole_day(string value, string expectedFrom, string expectedTo)
    {
        Assert.True(UtcInstant.TryParseFrom(value, out var from));
        Assert.True(UtcInstant.TryParseTo(value, out var to));
        Assert.Equal((DateTimeKind.Utc, DateTimeKind.Utc), (from.Kind, to.Kind));
        Assert.Equal(DateTime.Parse(expectedFrom, CultureInfo.InvariantCulture), from);
        Assert.Equal(DateTime.Parse(expectedTo, CultureInfo.InvariantCulture), to);
    }

    [Theory]
    [Trait("Rule", "R14")]
    [InlineData("2026-10-28T23:15:00")]
    [InlineData("2026-10-29T01:15:00+02:00")]
    [InlineData("2026-10-28T23:15:00.5Z")]
    [InlineData("28.10.2026")]
    [InlineData("")]
    [InlineData(null)]
    public void Anything_that_is_not_an_explicit_utc_instant_is_rejected(string? value)
    {
        Assert.False(UtcInstant.TryParseFrom(value, out _));
        Assert.False(UtcInstant.TryParseTo(value, out _));
    }

    [Fact]
    [Trait("Rule", "R14")]
    public void Instants_from_any_offset_are_stored_as_utc_whole_seconds()
    {
        var utc = UtcInstant.ToWholeSeconds(new DateTimeOffset(2026, 10, 29, 1, 15, 30, 750, TimeSpan.FromHours(2)));

        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(new DateTime(2026, 10, 28, 23, 15, 30, DateTimeKind.Utc), utc);
    }
}