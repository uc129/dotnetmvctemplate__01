using AppTemplate.Application.Common;
using AppTemplate.Domain.Common;
using AppTemplate.Domain.Customers;

namespace AppTemplate.Application.Customers;

/// <summary>
/// Declared here, implemented in Infrastructure (dependency inversion): the service depends on
/// this interface, so Application never references SQL Server or Dapper.
/// Writes return Result because a stored procedure can refuse them (duplicate email, stale version).
/// Reads return plain values: "not found" is null, and anything else is an exception.
/// </summary>
public interface ICustomerRepository
{
    Task<PagedResult<CustomerListItem>> GetPagedAsync(PageRequest request, CancellationToken cancellationToken);

    Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken);

    Task<Result<int>> InsertAsync(Customer customer, CancellationToken cancellationToken);

    Task<Result> UpdateAsync(Customer customer, byte[] expectedRowVersion, CancellationToken cancellationToken);
}
