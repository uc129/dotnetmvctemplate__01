using System.Text.Json;
using AppTemplate.Web.Common.Dates;

namespace AppTemplate.UnitTests.Common;

public sealed class DateJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    [Fact]
    public void Writes_dates_in_house_format_including_nullable()
    {
        var json = JsonSerializer.Serialize(new Sample(new DateOnly(2026, 11, 19), null), Options);

        Assert.Equal("""{"Day":"19-Nov-2026","Maybe":null}""", json);
    }

    [Fact]
    public void Rejects_iso_dates_on_the_way_in()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Sample>("""{"Day":"2026-11-19"}""", Options));
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        DateJsonConverters.AddTo(options.Converters);
        return options;
    }

    private sealed record Sample(DateOnly Day, DateOnly? Maybe);
}
