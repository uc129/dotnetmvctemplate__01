using AppTemplate.Application.Customers;
using AppTemplate.Domain.Customers;
using AppTemplate.UnitTests.Fakes;

namespace AppTemplate.UnitTests.Customers;

public sealed class CustomerServiceTests
{
    // 20:00 UTC on 18-Nov is 01:30 IST on 19-Nov: the business day has already changed.
    private static readonly DateTimeOffset LateEveningUtc = new(2026, 11, 18, 20, 0, 0, TimeSpan.Zero);

    private readonly FakeCustomerRepository _repository = new();

    [Fact]
    public async Task Create_uses_the_business_date_not_the_utc_date()
    {
        var service = new CustomerService(_repository, TestClock.At(LateEveningUtc));
        var input = new CreateCustomerInput("Asha", "asha@example.com", null, new DateOnly(2026, 11, 19));

        var result = await service.CreateAsync(input, TestContext.Current.CancellationToken);

        // In UTC this date is "tomorrow" and would be refused. In IST it is today.
        Assert.True(result.IsSuccess);
        Assert.Single(_repository.Inserted);
    }

    [Fact]
    public async Task Create_does_not_reach_the_repository_when_validation_fails()
    {
        var service = new CustomerService(_repository, TestClock.At(LateEveningUtc));

        var result = await service.CreateAsync(new CreateCustomerInput("", "x", null, new DateOnly(2026, 1, 1)), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Empty(_repository.Inserted);
    }

    [Fact]
    public async Task Update_of_missing_customer_returns_not_found()
    {
        var service = new CustomerService(_repository, TestClock.At(LateEveningUtc));
        var input = new UpdateCustomerInput("Asha", "asha@example.com", null, new DateOnly(2026, 1, 1), true, [1]);

        var result = await service.UpdateAsync(42, input, TestContext.Current.CancellationToken);

        Assert.Equal([CustomerErrors.NotFound], result.Errors);
    }
}
