using AppTemplate.Application.Common;
using AppTemplate.Application.Customers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace AppTemplate.IntegrationTests;

/// <summary>
/// Starts the real app in memory against the test database, in a non-Development environment
/// so the production error handling is what gets tested.
/// </summary>
public class AppFactory(DatabaseFixture database) : WebApplicationFactory<Program>
{
    /// <summary>10:00 IST on 19-Nov-2026.</summary>
    public static readonly DateTimeOffset Now = new(2026, 11, 19, 4, 30, 0, TimeSpan.Zero);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:ConnectionString", database.ConnectionString);
        builder.ConfigureTestServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(new FakeTimeProvider(Now)));
            ConfigureServices(services);
        });
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }
}

/// <summary>The app with a repository that always throws, to exercise the unexpected-error path.</summary>
public sealed class BrokenDatabaseFactory(DatabaseFixture database) : AppFactory(database)
{
    protected override void ConfigureServices(IServiceCollection services) =>
        services.Replace(ServiceDescriptor.Scoped<ICustomerRepository, ThrowingRepository>());

    private sealed class ThrowingRepository : ICustomerRepository
    {
        public const string Secret = "connection to db-prod-01 failed: password=hunter2";

        public Task<PagedResult<CustomerListItem>> GetPagedAsync(PageRequest request, CancellationToken cancellationToken) => throw new InvalidOperationException(Secret);

        public Task<AppTemplate.Domain.Customers.Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken) => throw new InvalidOperationException(Secret);

        public Task<AppTemplate.Domain.Common.Result<int>> InsertAsync(AppTemplate.Domain.Customers.Customer customer, CancellationToken cancellationToken) => throw new InvalidOperationException(Secret);

        public Task<AppTemplate.Domain.Common.Result> UpdateAsync(AppTemplate.Domain.Customers.Customer customer, byte[] expectedRowVersion, CancellationToken cancellationToken) => throw new InvalidOperationException(Secret);
    }
}
