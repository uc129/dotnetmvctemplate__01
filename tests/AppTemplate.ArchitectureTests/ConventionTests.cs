using System.Reflection;
using System.Text.RegularExpressions;
using AppTemplate.Infrastructure.Data;

namespace AppTemplate.ArchitectureTests;

/// <summary>
/// Standards the compiler cannot see. BannedSymbols.txt covers C# APIs; these cover strings
/// and SQL files.
/// </summary>
public sealed partial class ConventionTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Repositories_call_procedures_by_schema_qualified_name()
    {
        // Procedure names are const strings in repositories: "dbo.Customer_GetPaged".
        var names = typeof(DapperContext).Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Repository", StringComparison.Ordinal))
            .SelectMany(t => t.GetFields(BindingFlags.NonPublic | BindingFlags.Static))
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(names);
        Assert.All(names, n => Assert.Matches(ProcedureName(), n));
    }

    [Fact]
    public void Every_procedure_a_repository_calls_exists_in_the_database_project()
    {
        var called = typeof(DapperContext).Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Repository", StringComparison.Ordinal))
            .SelectMany(t => t.GetFields(BindingFlags.NonPublic | BindingFlags.Static))
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);

        var defined = SqlFiles()
            .SelectMany(f => CreateProcedure().Matches(File.ReadAllText(f)).Select(m => m.Groups["name"].Value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.All(called, name => Assert.Contains(name, defined));
    }

    [Fact]
    public void Write_procedures_set_xact_abort()
    {
        var writes = SqlFiles()
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"\b(INSERT|UPDATE|DELETE|MERGE)\b", RegexOptions.IgnoreCase)
                        && CreateProcedure().IsMatch(File.ReadAllText(f)));

        Assert.All(writes, f => Assert.Contains("SET XACT_ABORT ON", File.ReadAllText(f), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Sql_never_turns_dates_into_text()
    {
        // Dates are formatted in C# (DateFormat). CONVERT(VARCHAR, date, 106) or FORMAT() in SQL
        // would create a second format and break sorting.
        var offenders = SqlFiles()
            .Where(f => DateToText().IsMatch(File.ReadAllText(f)))
            .Select(Path.GetFileName);

        Assert.Empty(offenders);
    }

    private static IEnumerable<string> SqlFiles() =>
        Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "database"), "*.sql", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AppTemplate.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not find AppTemplate.sln above the test output folder.");
    }

    [GeneratedRegex(@"^[a-z]+\.[A-Z][A-Za-z]+_[A-Z][A-Za-z]+$")]
    private static partial Regex ProcedureName();

    [GeneratedRegex(@"CREATE\s+(OR\s+ALTER\s+)?PROC(EDURE)?\s+(?<name>[\w\.\[\]]+)", RegexOptions.IgnoreCase)]
    private static partial Regex CreateProcedure();

    [GeneratedRegex(@"\bFORMAT\s*\(|CONVERT\s*\(\s*N?VARCHAR[^,]*,[^,]+,\s*\d+\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex DateToText();
}
