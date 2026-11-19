using AppTemplate.Domain.Common;
using Microsoft.Data.SqlClient;

namespace AppTemplate.Infrastructure.Data;

/// <summary>
/// Turns the SQL errors that are business decisions into <see cref="Error"/>s. Everything else
/// stays an exception, because it means something is broken, not that the user did something wrong.
/// </summary>
/// <remarks>
/// Stored procedure convention (see database/README.md):
/// <code>THROW 50409, N'Customer.EmailTaken|Another customer already uses this email.', 1;</code>
/// The number picks the type: 50400 validation, 50404 not found, 50409 conflict.
/// The message is "Code|Text a user can read".
/// </remarks>
public static class SqlErrorTranslator
{
    public const int ValidationError = 50400;
    public const int NotFoundError = 50404;
    public const int ConflictError = 50409;

    private const int UniqueConstraintViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    public static bool TryTranslate(SqlException exception, out Error error)
    {
        switch (exception.Number)
        {
            case ValidationError:
                error = Parse(exception.Message, ErrorType.Validation);
                return true;
            case NotFoundError:
                error = Parse(exception.Message, ErrorType.NotFound);
                return true;
            case ConflictError:
                error = Parse(exception.Message, ErrorType.Conflict);
                return true;
            // The procedure should have checked first; this catches the race where two users save at once.
            case UniqueConstraintViolation or UniqueIndexViolation:
                error = Error.Conflict("Database.Duplicate", "This record already exists.");
                return true;
            default:
                error = null!;
                return false;
        }
    }

    private static Error Parse(string message, ErrorType type)
    {
        var separator = message.IndexOf('|', StringComparison.Ordinal);
        return separator > 0
            ? new Error(message[..separator], message[(separator + 1)..], type)
            : new Error("Database.Rule", message, type);
    }
}
