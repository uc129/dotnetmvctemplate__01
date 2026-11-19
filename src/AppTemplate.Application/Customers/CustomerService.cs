using AppTemplate.Application.Common;
using AppTemplate.Domain.Common;
using AppTemplate.Domain.Customers;

namespace AppTemplate.Application.Customers;

/// <summary>
/// Customer use cases. Controllers call this; it applies domain rules and calls the repository.
/// There is no ICustomerService: an interface earns its place when there is a second
/// implementation or a boundary to cross, and this class has neither. Tests use the real
/// class with a fake repository.
/// </summary>
public sealed class CustomerService(ICustomerRepository repository, BusinessClock clock)
{
    public Task<PagedResult<CustomerListItem>> GetPagedAsync(PageRequest request, CancellationToken cancellationToken) =>
        repository.GetPagedAsync(request, cancellationToken);

    public async Task<Result<CustomerDetails>> GetAsync(int customerId, CancellationToken cancellationToken)
    {
        var customer = await repository.GetByIdAsync(customerId, cancellationToken);
        return customer is null ? CustomerErrors.NotFound : ToDetails(customer);
    }

    public async Task<Result<int>> CreateAsync(CreateCustomerInput input, CancellationToken cancellationToken)
    {
        var created = Customer.Create(input.Name, input.Email, input.Phone, input.CustomerSince, clock.Today);
        if (created.IsFailure)
        {
            return Result<int>.Failure(created.Errors);
        }

        return await repository.InsertAsync(created.Value, cancellationToken);
    }

    public async Task<Result> UpdateAsync(int customerId, UpdateCustomerInput input, CancellationToken cancellationToken)
    {
        var customer = await repository.GetByIdAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        var updated = customer.Update(input.Name, input.Email, input.Phone, input.CustomerSince, input.IsActive, clock.Today);
        if (updated.IsFailure)
        {
            return updated;
        }

        return await repository.UpdateAsync(customer, input.RowVersion, cancellationToken);
    }

    private static CustomerDetails ToDetails(Customer c) =>
        new(c.Id, c.Name, c.Email, c.Phone, c.CustomerSince, c.IsActive, c.RowVersion);
}
