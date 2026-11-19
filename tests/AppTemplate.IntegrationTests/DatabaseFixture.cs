using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(AppTemplate.IntegrationTests.DatabaseFixture))]

namespace AppTemplate.IntegrationTests;

/// <summary>
/// One SQL Server container for the whole test run, with the schema deployed from the dacpac:
/// the same artifact that goes to production, so the tests check what will actually ship.
/// Tests share the database and keep apart by using unique data (see Unique.Tag).
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string DatabaseName = "AppTemplate";

    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        using var package = DacPackage.Load(FindDacpac());
        new DacServices(_container.GetConnectionString())
            .Deploy(package, DatabaseName, upgradeExisting: true, new DacDeployOptions { CreateNewDatabase = true });

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString()) { InitialCatalog = DatabaseName }.ConnectionString;
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    private static string FindDacpac()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AppTemplate.sln")))
        {
            dir = dir.Parent;
        }

        var bin = Path.Combine(dir?.FullName ?? throw new InvalidOperationException("AppTemplate.sln not found."), "database", "AppTemplate.Database", "bin");
        return Directory.EnumerateFiles(bin, "AppTemplate.Database.dacpac", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault() ?? throw new InvalidOperationException($"No dacpac under {bin}. Build the database project first.");
    }
}

internal static class Unique
{
    /// <summary>A short random tag, so tests running in parallel never see each other's rows.</summary>
    public static string Tag() => Guid.NewGuid().ToString("N")[..8];
}
