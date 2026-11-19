using System.Data;
using System.Diagnostics;
using AppTemplate.Application.Common;
using AppTemplate.Domain.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppTemplate.Infrastructure.Data;

#pragma warning disable RS0030 // DapperContext is the one class allowed to use SqlConnection and Dapper directly.

/// <summary>
/// The only way to reach the database. Every call is a stored procedure (CommandType is fixed),
/// every call is timed and logged, and SQL business errors become Results in one place.
/// </summary>
/// <remarks>
/// Lifetime: scoped (one per HTTP request). The connection opens on first use and closes when the
/// request ends. One connection per request is what makes a transaction across several
/// repositories possible later. The rule that comes with it: never run two calls on the same
/// context at the same time (no Task.WhenAll over repository calls); a SqlConnection runs one
/// command at a time.
/// </remarks>
public sealed partial class DapperContext(IOptions<DatabaseOptions> options, ILogger<DapperContext> logger) : IAsyncDisposable, IDisposable
{
    private readonly DatabaseOptions _options = options.Value;

    // Dapper's type map is global and static, so the handler is registered once per process.
    static DapperContext() => SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    private SqlConnection? _connection;

    /// <summary>Rows from a procedure. Read procedures do not THROW business errors: no rows means not found.</summary>
    public Task<IReadOnlyList<T>> QueryAsync<T>(string procedure, object? parameters, CancellationToken cancellationToken) =>
        RunAsync(procedure, parameters, async (connection, command) =>
        {
            var rows = await connection.QueryAsync<T>(command);
            return (IReadOnlyList<T>)rows.AsList();
        }, cancellationToken);

    public Task<T?> QuerySingleOrDefaultAsync<T>(string procedure, object? parameters, CancellationToken cancellationToken) =>
        RunAsync(procedure, parameters, (connection, command) => connection.QuerySingleOrDefaultAsync<T>(command), cancellationToken);

    /// <summary>
    /// The paging convention: result set 1 is the page of rows, result set 2 is one row with
    /// TotalCount and FilteredCount. Every *_GetPaged procedure follows it.
    /// </summary>
    public Task<PagedResult<T>> QueryPagedAsync<T>(string procedure, object? parameters, CancellationToken cancellationToken) =>
        RunAsync(procedure, parameters, async (connection, command) =>
        {
            using var grid = await connection.QueryMultipleAsync(command);
            var rows = (await grid.ReadAsync<T>()).AsList();
            var counts = await grid.ReadSingleAsync<PageCounts>();
            return new PagedResult<T>(rows, counts.TotalCount, counts.FilteredCount);
        }, cancellationToken);

    /// <summary>A write that returns one value (usually the new id), or the business error the procedure raised.</summary>
    public async Task<Result<T>> ExecuteScalarAsync<T>(string procedure, object? parameters, CancellationToken cancellationToken)
    {
        try
        {
            var value = await RunAsync(procedure, parameters, (connection, command) => connection.ExecuteScalarAsync<T>(command), cancellationToken);
            return value is null
                ? throw new InvalidOperationException($"{procedure} returned no value.")
                : Result<T>.Success(value);
        }
        catch (SqlException ex) when (SqlErrorTranslator.TryTranslate(ex, out var error))
        {
            LogRefused(procedure, error.Code);
            return error;
        }
    }

    /// <summary>A write with no return value, or the business error the procedure raised.</summary>
    public async Task<Result> ExecuteAsync(string procedure, object? parameters, CancellationToken cancellationToken)
    {
        try
        {
            await RunAsync(procedure, parameters, (connection, command) => connection.ExecuteAsync(command), cancellationToken);
            return Result.Success();
        }
        catch (SqlException ex) when (SqlErrorTranslator.TryTranslate(ex, out var error))
        {
            LogRefused(procedure, error.Code);
            return error;
        }
    }

    public void Dispose() => _connection?.Dispose();

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    private async Task<TResult> RunAsync<TResult>(
        string procedure,
        object? parameters,
        Func<SqlConnection, CommandDefinition, Task<TResult>> call,
        CancellationToken cancellationToken)
    {
        var connection = await GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            procedure,
            parameters,
            commandType: CommandType.StoredProcedure,
            commandTimeout: _options.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        var started = Stopwatch.GetTimestamp();
        var result = await call(connection, command);
        var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        if (elapsed >= _options.SlowCallMilliseconds)
        {
            LogSlow(procedure, elapsed);
        }
        else
        {
            LogExecuted(procedure, elapsed);
        }

        return result;
    }

    private async Task<SqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            _connection = new SqlConnection(_options.ConnectionString);
            await _connection.OpenAsync(cancellationToken);
        }

        return _connection;
    }

    // Source-generated logging: no boxing, no string formatting when the level is off.
    // Parameter values are never logged: they can hold personal data.
    [LoggerMessage(Level = LogLevel.Debug, Message = "Executed {Procedure} in {ElapsedMs:0} ms")]
    private partial void LogExecuted(string procedure, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Slow call: {Procedure} took {ElapsedMs:0} ms")]
    private partial void LogSlow(string procedure, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Procedure} refused the request: {ErrorCode}")]
    private partial void LogRefused(string procedure, string errorCode);

    private sealed record PageCounts(int TotalCount, int FilteredCount);
}
#pragma warning restore RS0030
