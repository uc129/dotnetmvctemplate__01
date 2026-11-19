using AppTemplate.Application.Common;
using AppTemplate.Domain.Common;
using AppTemplate.Domain.Customers;
using AppTemplate.Infrastructure.Customers;
using AppTemplate.Infrastructure.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AppTemplate.IntegrationTests;

/// <summary>The repository against the real stored procedures: mapping, THROW translation, paging, concurrency.</summary>
public sealed class CustomerRepositoryTests(DatabaseFixture database) : IAsyncDisposable
{
    private static readonly DateOnly Today = new(2026, 11, 19);

    private readonly DapperContext _db = new(
        Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString }),
        NullLogger<DapperContext>.Instance);

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Insert_then_get_round_trips_every_field()
    {
        var repository = new CustomerRepository(_db);
        var email = $"{Unique.Tag()}@example.com";

        var id = await repository.InsertAsync(NewCustomer("Asha Rao", email, new DateOnly(2024, 2, 29)), Ct);
        var loaded = await repository.GetByIdAsync(id.Value, Ct);

        Assert.NotNull(loaded);
        Assert.Equal(("Asha Rao", email, new DateOnly(2024, 2, 29), true), (loaded.Name, loaded.Email, loaded.CustomerSince, loaded.IsActive));
        Assert.Equal(8, loaded.RowVersion.Length);
    }

    [Fact]
    public async Task Duplicate_email_comes_back_as_a_conflict_from_the_procedure()
    {
        var repository = new CustomerRepository(_db);
        var email = $"{Unique.Tag()}@example.com";
        await repository.InsertAsync(NewCustomer("First", email), Ct);

        var second = await repository.InsertAsync(NewCustomer("Second", email), Ct);

        var error = Assert.Single(second.Errors);
        Assert.Equal(("Customer.EmailTaken", ErrorType.Conflict), (error.Code, error.Type));
    }

    [Fact]
    public async Task Saving_with_a_stale_row_version_is_refused()
    {
        var repository = new CustomerRepository(_db);
        var id = (await repository.InsertAsync(NewCustomer("Asha", $"{Unique.Tag()}@example.com"), Ct)).Value;
        var userA = (await repository.GetByIdAsync(id, Ct))!;
        var userB = (await repository.GetByIdAsync(id, Ct))!;

        userA.Update("Asha (A)", userA.Email, null, userA.CustomerSince, true, Today);
        Assert.True((await repository.UpdateAsync(userA, userA.RowVersion, Ct)).IsSuccess);

        // B still holds the version from before A saved.
        userB.Update("Asha (B)", userB.Email, null, userB.CustomerSince, true, Today);
        var result = await repository.UpdateAsync(userB, userB.RowVersion, Ct);

        Assert.Equal("Customer.Stale", Assert.Single(result.Errors).Code);
        Assert.Equal("Asha (A)", (await repository.GetByIdAsync(id, Ct))!.Name);
    }

    [Fact]
    public async Task Updating_a_missing_customer_returns_not_found_from_the_procedure()
    {
        var repository = new CustomerRepository(_db);
        var ghost = Customer.Rehydrate(int.MaxValue, "Ghost", "ghost@example.com", null, Today, true, new byte[8]);

        var result = await repository.UpdateAsync(ghost, ghost.RowVersion, Ct);

        Assert.Equal(("Customer.NotFound", ErrorType.NotFound), (result.Errors[0].Code, result.Errors[0].Type));
    }

    [Fact]
    public async Task Paging_searches_sorts_and_counts()
    {
        var repository = new CustomerRepository(_db);
        var tag = Unique.Tag();
        foreach (var (name, since) in new[] { ("B", new DateOnly(2025, 1, 1)), ("A", new DateOnly(2026, 6, 1)), ("C", new DateOnly(2024, 3, 15)) })
        {
            await repository.InsertAsync(NewCustomer($"{tag} {name}", $"{tag}{name}@example.com", since), Ct);
        }

        var page = await repository.GetPagedAsync(PageRequest.Create(0, 2, tag, "customerSince", sortDescending: true), Ct);

        Assert.Equal(3, page.FilteredCount);
        Assert.True(page.TotalCount >= 3);
        Assert.Equal([$"{tag} A", $"{tag} B"], page.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task Search_treats_like_wildcards_as_text()
    {
        var repository = new CustomerRepository(_db);
        var tag = Unique.Tag();
        await repository.InsertAsync(NewCustomer($"{tag} 50% off", $"{tag}a@example.com"), Ct);
        await repository.InsertAsync(NewCustomer($"{tag} 500 units", $"{tag}b@example.com"), Ct);

        var page = await repository.GetPagedAsync(PageRequest.Create(0, 10, $"{tag} 50%", null, false), Ct);

        Assert.Equal($"{tag} 50% off", Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task Unknown_sort_column_is_ignored_not_executed()
    {
        var repository = new CustomerRepository(_db);

        var page = await repository.GetPagedAsync(PageRequest.Create(0, 5, null, "Name; DROP TABLE dbo.Customer;--", false), Ct);

        Assert.True(page.TotalCount >= 0);
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private static Customer NewCustomer(string name, string email, DateOnly? since = null) =>
        Customer.Create(name, email, null, since ?? Today, Today).Value;
}
