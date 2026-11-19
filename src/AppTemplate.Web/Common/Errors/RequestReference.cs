using System.Diagnostics;

namespace AppTemplate.Web.Common.Errors;

/// <summary>
/// The reference shown to users when something fails. It is the W3C trace id, which Serilog
/// writes on every log line of the request, so support can search the logs for exactly it.
/// </summary>
public static class RequestReference
{
    public static string Get(HttpContext httpContext) =>
        Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
}
