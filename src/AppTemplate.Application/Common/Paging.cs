namespace AppTemplate.Application.Common;

/// <summary>
/// One page of a list, as the UI asks for it. Not tied to DataTables: the web layer translates.
/// </summary>
public sealed record PageRequest
{
    public const int MaxLength = 100;

    private PageRequest(int start, int length, string? search, string? sortColumn, bool sortDescending)
    {
        Start = start;
        Length = length;
        Search = search;
        SortColumn = sortColumn;
        SortDescending = sortDescending;
    }

    public int Start { get; }

    public int Length { get; }

    public string? Search { get; }

    /// <summary>
    /// A column name the stored procedure knows. It is passed as a parameter, never concatenated
    /// into SQL; the procedure's ORDER BY CASE is the whitelist and ignores unknown names.
    /// </summary>
    public string? SortColumn { get; }

    public bool SortDescending { get; }

    /// <summary>Clamps bad input (negative start, length of -1 for "all") instead of trusting the browser.</summary>
    public static PageRequest Create(int start, int length, string? search, string? sortColumn, bool sortDescending) =>
        new(
            Math.Max(start, 0),
            length is < 1 or > MaxLength ? MaxLength : length,
            string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            string.IsNullOrWhiteSpace(sortColumn) ? null : sortColumn,
            sortDescending);
}

/// <param name="Items">The rows on this page.</param>
/// <param name="TotalCount">All rows, ignoring the search.</param>
/// <param name="FilteredCount">Rows matching the search, across all pages.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int FilteredCount);
