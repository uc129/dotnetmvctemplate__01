using AppTemplate.Application.Common;
using AppTemplate.Web.Common.DataTables;

namespace AppTemplate.UnitTests.Common;

public sealed class PagingTests
{
    [Theory]
    [InlineData(-1, 100)]   // DataTables sends -1 for "show all"
    [InlineData(0, 100)]
    [InlineData(5000, 100)]
    [InlineData(25, 25)]
    public void Page_length_is_clamped(int requested, int expected)
    {
        Assert.Equal(expected, PageRequest.Create(0, requested, null, null, false).Length);
    }

    [Fact]
    public void DataTables_request_maps_the_sorted_column_to_its_data_name()
    {
        var request = new DataTableRequest
        {
            Start = 50,
            Length = 25,
            Search = new DataTableSearch { Value = "  rao " },
            Columns = [new() { Data = "name", Orderable = true }, new() { Data = "customerSince", Orderable = true }],
            Order = [new() { Column = 1, Dir = "desc" }],
        };

        var page = request.ToPageRequest();

        Assert.Equal(50, page.Start);
        Assert.Equal("rao", page.Search);
        Assert.Equal("customerSince", page.SortColumn);
        Assert.True(page.SortDescending);
    }

    [Theory]
    [InlineData(5)]    // index out of range
    [InlineData(-1)]
    [InlineData(1)]    // column not orderable
    public void Bad_or_unsortable_order_column_means_no_sort_column(int column)
    {
        var request = new DataTableRequest
        {
            Length = 10,
            Columns = [new() { Data = "name", Orderable = true }, new() { Data = "phone", Orderable = false }],
            Order = [new() { Column = column, Dir = "asc" }],
        };

        Assert.Null(request.ToPageRequest().SortColumn);
    }
}
