using AppTemplate.Application.Common;
using AppTemplate.Application.Customers;
using AppTemplate.Domain.Common;
using AppTemplate.Domain.Customers;

namespace AppTemplate.UnitTests.Fakes;

/// <summary>
/// A hand-written fake instead of a mocking library: it is plain code you can read and debug,
/// and the tests check what happened (state) rather than which methods were called.
/// </summary>
internal sealed class FakeCustomerRepository : ICustomerRepository
{
    public List<Customer> Inserted { get; } = [];

    public List<Customer> Updated { get; } = [];

    public Dictionary<int, Customer> Stored { get; } = [];

    public Task<PagedResult<CustomerListItem>> GetPagedAsync(PageRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new PagedResult<CustomerListItem>([], 0, 0));

    public Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken) =>
        Task.FromResult(Stored.GetValueOrDefault(customerId));

    public Task<Result<int>> InsertAsync(Customer customer, CancellationToken cancellationToken)
    {
        Inserted.Add(customer);
        return Task.FromResult(Result.Success(Inserted.Count));
    }

    public Task<Result> UpdateAsync(Customer customer, byte[] expectedRowVersion, CancellationToken cancellationToken)
    {
        Updated.Add(customer);
        return Task.FromResult(Result.Success());
    }
}
