using AppTemplate.Application.Common;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace AppTemplate.UnitTests.Fakes;

internal static class TestClock
{
    /// <summary>A BusinessClock in IST whose UTC "now" the test chooses.</summary>
    public static BusinessClock At(DateTimeOffset utcNow) =>
        new(new FakeTimeProvider(utcNow), Options.Create(new BusinessTimeOptions { TimeZone = "Asia/Kolkata" }));
}
