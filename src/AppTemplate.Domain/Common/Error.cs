namespace AppTemplate.Domain.Common;

/// <summary>What kind of failure an <see cref="Error"/> is. The web layer maps it to an HTTP status.</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
}

/// <summary>
/// An expected failure: bad input, a missing record, a rule a stored procedure refused.
/// Unexpected failures (a dropped connection, a bug) are exceptions and never become an Error.
/// </summary>
/// <param name="Code">Stable machine-readable code, e.g. <c>Customer.EmailTaken</c>. Never shown to users; safe to log and test against.</param>
/// <param name="Message">Text a user can read.</param>
/// <param name="Type">Category, used to choose the response.</param>
/// <param name="Field">The input field this error belongs to, when there is one (form validation).</param>
public sealed record Error(string Code, string Message, ErrorType Type, string? Field = null)
{
    public static Error Validation(string code, string message, string? field = null) =>
        new(code, message, ErrorType.Validation, field);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}
