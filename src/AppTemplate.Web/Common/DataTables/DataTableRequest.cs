using AppTemplate.Application.Common;

namespace AppTemplate.Web.Common.DataTables;

/// <summary>
/// What DataTables posts in server-side mode (sent as JSON by site.js, see app.dataTable).
/// Translated into a <see cref="PageRequest"/> so nothing below the web layer knows about DataTables.
/// </summary>
public sealed class DataTableRequest
{
    public int Draw { get; set; }

    public int Start { get; set; }

    public int Length { get; set; }

    public DataTableSearch? Search { get; set; }

    public List<DataTableOrder> Order { get; set; } = [];

    public List<DataTableColumn> Columns { get; set; } = [];

    public PageRequest ToPageRequest()
    {
        var order = Order.FirstOrDefault();
        var column = order is not null && order.Column >= 0 && order.Column < Columns.Count && Columns[order.Column].Orderable
            ? Columns[order.Column].Data
            : null;
        var descending = string.Equals(order?.Dir, "desc", StringComparison.OrdinalIgnoreCase);

        return PageRequest.Create(Start, Length, Search?.Value, column, descending);
    }
}

public sealed class DataTableSearch
{
    public string? Value { get; set; }
}

public sealed class DataTableOrder
{
    public int Column { get; set; }

    public string? Dir { get; set; }
}

public sealed class DataTableColumn
{
    /// <summary>The JSON property the column shows, e.g. "customerSince". Sent to the procedure as the sort column.</summary>
    public string? Data { get; set; }

    public bool Orderable { get; set; }
}

/// <summary>The response shape DataTables expects. Property names serialize to camelCase.</summary>
public sealed record DataTableResponse<T>(int Draw, int RecordsTotal, int RecordsFiltered, IReadOnlyList<T> Data)
{
    public static DataTableResponse<T> From(int draw, PagedResult<T> page) =>
        new(draw, page.TotalCount, page.FilteredCount, page.Items);
}
