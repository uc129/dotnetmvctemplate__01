using Microsoft.Extensions.Options;

namespace AppTemplate.Application.Common;

public sealed class BusinessTimeOptions
{
    public const string SectionName = "BusinessTime";

    /// <summary>IANA id, e.g. "Asia/Kolkata". Works on Windows and Linux in .NET 6+.</summary>
    public string TimeZone { get; set; } = "Asia/Kolkata";
}

/// <summary>
/// "Today" for the business, not for the server. A server set to UTC would say it is still
/// yesterday until 05:30 IST; this class removes that bug from every caller.
/// Inject this instead of reading the system clock. Tests pass a FakeTimeProvider.
/// </summary>
public sealed class BusinessClock(TimeProvider timeProvider, IOptions<BusinessTimeOptions> options)
{
    private readonly TimeZoneInfo _zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);

    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _zone);
}
