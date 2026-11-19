using AppTemplate.Application.Common;
using AppTemplate.Application.Customers;
using AppTemplate.Domain.Common;
using AppTemplate.Domain.Customers;
using AppTemplate.Infrastructure.Data;

namespace AppTemplate.Infrastructure.Customers;

/// <summary>
/// One method per stored procedure. Procedure names are constants so a rename is one edit and
/// a search for the procedure name finds its only caller.
/// </summary>
public sealed class CustomerRepository(DapperContext db) : ICustomerRepository
{
    private const string GetPaged = "dbo.Customer_GetPaged";
    private const string GetById = "dbo.Customer_GetById";
    private const string Insert = "dbo.Customer_Insert";
    private const string Update = "dbo.Customer_Update";

    public async Task<PagedResult<CustomerListItem>> GetPagedAsync(PageRequest request, CancellationToken cancellationToken)
    {
        var page = await db.QueryPagedAsync<CustomerRow>(
            GetPaged,
            new
            {
                request.Start,
                request.Length,
                request.Search,
                request.SortColumn,
                SortDirection = request.SortDescending ? "DESC" : "ASC",
            },
            cancellationToken);

        var items = page.Items.Select(r => new CustomerListItem(r.CustomerId, r.Name, r.Email, r.Phone, r.CustomerSince, r.IsActive)).ToList();
        return new PagedResult<CustomerListItem>(items, page.TotalCount, page.FilteredCount);
    }

    public async Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken)
    {
        var row = await db.QuerySingleOrDefaultAsync<CustomerRow>(GetById, new { CustomerId = customerId }, cancellationToken);
        return row is null
            ? null
            : Customer.Rehydrate(row.CustomerId, row.Name, row.Email, row.Phone, row.CustomerSince, row.IsActive, row.RowVersion);
    }

    public Task<Result<int>> InsertAsync(Customer customer, CancellationToken cancellationToken) =>
        db.ExecuteScalarAsync<int>(
            Insert,
            new { customer.Name, customer.Email, customer.Phone, customer.CustomerSince },
            cancellationToken);

    public Task<Result> UpdateAsync(Customer customer, byte[] expectedRowVersion, CancellationToken cancellationToken) =>
        db.ExecuteAsync(
            Update,
            new
            {
                CustomerId = customer.Id,
                customer.Name,
                customer.Email,
                customer.Phone,
                customer.CustomerSince,
                customer.IsActive,
                RowVersion = expectedRowVersion,
            },
            cancellationToken);

    // The shape of the procedures' result sets. Kept private: it exists only to cross the SQL
    // boundary. A class with init properties, not a record, because of DateOnlyTypeHandler's limitation.
    private sealed class CustomerRow
    {
        public int CustomerId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string? Phone { get; init; }

        public DateOnly CustomerSince { get; init; }

        public bool IsActive { get; init; }

        public byte[] RowVersion { get; init; } = [];
    }
}
