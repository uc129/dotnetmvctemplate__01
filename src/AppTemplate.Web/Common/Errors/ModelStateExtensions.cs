using AppTemplate.Domain.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AppTemplate.Web.Common.Errors;

public static class ModelStateExtensions
{
    /// <summary>
    /// Shows Result errors on the form: errors with a Field go next to that input, the rest
    /// (conflicts, stale edits) go in the validation summary at the top.
    /// </summary>
    public static void AddErrors(this ModelStateDictionary modelState, IEnumerable<Error> errors)
    {
        foreach (var error in errors)
        {
            modelState.AddModelError(error.Field ?? string.Empty, error.Message);
        }
    }
}

public static class ResultExtensions
{
    public static bool IsNotFound(this Result result) => result.Errors.Any(e => e.Type == ErrorType.NotFound);
}
