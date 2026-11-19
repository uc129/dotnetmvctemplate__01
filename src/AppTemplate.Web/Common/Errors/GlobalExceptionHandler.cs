using Microsoft.AspNetCore.Diagnostics;

namespace AppTemplate.Web.Common.Errors;

/// <summary>
/// The one place unexpected exceptions are logged. Nothing below the web layer catches an
/// exception just to log it and rethrow; that writes the same error two or three times.
/// The response is not chosen here: UseExceptionHandler re-executes /Error, and
/// <see cref="Controllers.ErrorController"/> returns JSON or a page, so 500s and 404s follow one path.
/// </summary>
public sealed partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // By now Request.Path is already /Error; the feature keeps the path that actually failed.
        var path = httpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path ?? httpContext.Request.Path.Value;

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // The browser went away (closed tab, DataTables cancelled a search). Not an error.
            LogCancelled(httpContext.Request.Method, path);
            httpContext.Response.StatusCode = 499;
            return ValueTask.FromResult(true);
        }

        LogUnhandled(exception, httpContext.Request.Method, path);
        return ValueTask.FromResult(false);
    }

    /// <summary>True for jQuery AJAX (DataTables included) and anything asking for JSON.</summary>
    public static bool IsAjax(HttpRequest request) =>
        request.Headers.XRequestedWith == "XMLHttpRequest"
        || request.Headers.Accept.Any(a => a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception on {Method} {Path}")]
    private partial void LogUnhandled(Exception exception, string method, string? path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request cancelled by the client: {Method} {Path}")]
    private partial void LogCancelled(string method, string? path);
}
