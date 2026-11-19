namespace AppTemplate.Infrastructure.Data;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Set in user-secrets locally and in environment variables on servers. Never committed.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Calls slower than this are logged as warnings.</summary>
    public int SlowCallMilliseconds { get; set; } = 1000;

    public int CommandTimeoutSeconds { get; set; } = 30;
}
