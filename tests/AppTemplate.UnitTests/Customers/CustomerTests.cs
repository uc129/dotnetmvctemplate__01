using AppTemplate.Domain.Customers;

namespace AppTemplate.UnitTests.Customers;

public sealed class CustomerTests
{
    private static readonly DateOnly Today = new(2026, 11, 19);

    [Fact]
    public void Create_with_valid_input_trims_and_lowercases_email()
    {
        var result = Customer.Create("  Asha Rao ", " Asha@Example.COM ", "  ", Today, Today);

        Assert.True(result.IsSuccess);
        Assert.Equal("Asha Rao", result.Value.Name);
        Assert.Equal("asha@example.com", result.Value.Email);
        Assert.Null(result.Value.Phone);
    }

    [Fact]
    public void Create_reports_every_broken_rule_at_once()
    {
        var result = Customer.Create("", "not-an-email", null, Today.AddDays(1), Today);

        Assert.True(result.IsFailure);
        Assert.Equal(
            [CustomerErrors.NameRequired, CustomerErrors.EmailInvalid, CustomerErrors.CustomerSinceInFuture],
            result.Errors);
    }

    [Theory]
    [InlineData("a@b.co", true)]
    [InlineData("a.b@c.d.e", true)]
    [InlineData("@b.co", false)]
    [InlineData("a@b", false)]
    [InlineData("a@@b.co", false)]
    [InlineData("a b@c.co", false)]
    [InlineData("a@b.co.", false)]
    public void Create_checks_email_shape(string email, bool valid)
    {
        var result = Customer.Create("Name", email, null, Today, Today);

        Assert.Equal(valid, result.IsSuccess);
    }

    [Fact]
    public void Reading_the_value_of_a_failed_result_throws()
    {
        var result = Customer.Create(null, null, null, Today, Today);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
