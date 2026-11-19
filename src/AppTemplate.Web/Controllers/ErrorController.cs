using AppTemplate.Web.Common.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AppTemplate.Web.Controllers;

/// <summary>
/// Every error response comes from here: unhandled exceptions (re-executed by UseExceptionHandler)
/// and empty 4xx/5xx responses (re-executed by UseStatusCodePages). AJAX callers get
/// ProblemDetails JSON, browsers get a page. Neither ever contains exception details.
/// Logging already happened in GlobalExceptionHandler.
/// </summary>
[Route("Error")]
[ApiExplorerSettings(IgnoreApi = true)]
[IgnoreAntiforgeryToken] // Re-executed requests keep their original verb; the error page itself must always render.
public sealed class ErrorController : Controller
{
    [Route("")]
    public IActionResult Index() =>
        Respond(StatusCodes.Status500InternalServerError, "Something went wrong. Please try again, or quote the reference to support.");

    [Route("{statusCode:int}")]
    public IActionResult Status(int statusCode) => Respond(statusCode, statusCode switch
    {
        400 => "The request was not valid. Reload the page and try again.",
        403 => "You do not have access to this page.",
        404 => "The page or record you asked for does not exist.",
        _ => "The request could not be completed.",
    });

    private IActionResult Respond(int statusCode, string message)
    {
        if (GlobalExceptionHandler.IsAjax(Request))
        {
            return Problem(title: message, statusCode: statusCode);
        }

        Response.StatusCode = statusCode;
        return View("Error", new ErrorViewModel(statusCode, message, RequestReference.Get(HttpContext)));
    }
}

public sealed record ErrorViewModel(int StatusCode, string Message, string Reference);
