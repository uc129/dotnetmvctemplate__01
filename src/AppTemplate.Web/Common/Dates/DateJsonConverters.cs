using System.Text.Json;
using System.Text.Json.Serialization;
using AppTemplate.Application.Common;

namespace AppTemplate.Web.Common.Dates;

/// <summary>
/// JSON uses the house format too, so JavaScript receives "19-Nov-2026" and never formats dates
/// itself. One format on the screen, in forms and on the wire means nothing has to convert.
/// Nullable dates use these automatically.
/// </summary>
public static class DateJsonConverters
{
    public static void AddTo(IList<JsonConverter> converters)
    {
        converters.Add(new DateOnlyConverter());
        converters.Add(new DateTimeConverter());
    }

    private sealed class DateOnlyConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            DateFormat.TryParseDate(reader.GetString(), out var date)
                ? date.Value
                : throw new JsonException($"Dates must be {DateFormat.Date}, for example 19-Nov-2026.");

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToDisplay());
    }

    private sealed class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            DateFormat.TryParseDateTime(reader.GetString(), out var dateTime)
                ? dateTime.Value
                : throw new JsonException($"Date-times must be {DateFormat.DateTime}, for example 19-Nov-2026 14:05.");

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToDisplay());
    }
}
