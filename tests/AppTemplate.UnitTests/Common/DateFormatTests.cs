using System.Globalization;
using AppTemplate.Application.Common;

namespace AppTemplate.UnitTests.Common;

public sealed class DateFormatTests
{
    [Theory]
    [InlineData("19-Nov-2026")]
    [InlineData(" 19-Nov-2026 ")]
    [InlineData("19-nov-2026")]
    public void Parses_the_house_format(string text)
    {
        Assert.True(DateFormat.TryParseDate(text, out var date));
        Assert.Equal(new DateOnly(2026, 11, 19), date);
    }

    [Theory]
    [InlineData("2026-11-19")]   // ISO, what <input type="date"> sends
    [InlineData("19/11/2026")]
    [InlineData("11/19/2026")]
    [InlineData("19-11-2026")]
    [InlineData("31-Feb-2026")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_everything_else(string? text)
    {
        Assert.False(DateFormat.TryParseDate(text, out _));
    }

    [Fact]
    public void Output_does_not_depend_on_the_server_culture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR"); // would print "19-nov.-2026"
            Assert.Equal("19-Nov-2026", new DateOnly(2026, 11, 19).ToDisplay());
            Assert.Equal("05-Jan-2026 14:05", new DateTime(2026, 1, 5, 14, 5, 0, DateTimeKind.Unspecified).ToDisplay());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
