using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace AppTemplate.Application.Common;

/// <summary>
/// The single place dates become text and text becomes dates. Everything else (model binding,
/// JSON, views, the date picker) calls this. BannedSymbols.txt makes the alternatives compile errors.
/// </summary>
/// <remarks>
/// InvariantCulture matters: "MMM" gives "Nov" on every server, whatever its Windows region is set to.
/// Parsing is exact: "19-Nov-2026" is accepted, "19/11/2026" and "2026-11-19" are not, so a
/// wrongly formatted input fails loudly instead of being read as the wrong day.
/// </remarks>
public static class DateFormat
{
    /// <summary>The house date format, e.g. 19-Nov-2026.</summary>
    public const string Date = "dd-MMM-yyyy";

    /// <summary>Date and time, e.g. 19-Nov-2026 14:05. 24-hour clock.</summary>
    public const string DateTime = "dd-MMM-yyyy HH:mm";

    /// <summary>The same format in jQuery UI datepicker notation (M = short month name, yy = 4-digit year).</summary>
    public const string DatePickerFormat = "dd-M-yy";

#pragma warning disable RS0030 // This class is the one sanctioned place for date formatting and parsing.
    public static string ToDisplay(this DateOnly date) => date.ToString(Date, CultureInfo.InvariantCulture);

    public static string ToDisplay(this DateOnly? date) => date?.ToDisplay() ?? string.Empty;

    public static string ToDisplay(this DateTime dateTime) => dateTime.ToString(DateTime, CultureInfo.InvariantCulture);

    public static bool TryParseDate(string? text, [NotNullWhen(true)] out DateOnly? date)
    {
        if (DateOnly.TryParseExact(text?.Trim(), Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            date = parsed;
            return true;
        }

        date = null;
        return false;
    }

    public static bool TryParseDateTime(string? text, [NotNullWhen(true)] out DateTime? dateTime)
    {
        if (System.DateTime.TryParseExact(text?.Trim(), DateTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            dateTime = parsed;
            return true;
        }

        dateTime = null;
        return false;
    }
#pragma warning restore RS0030
}
