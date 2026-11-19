using AppTemplate.Domain.Common;

namespace AppTemplate.Domain.Customers;

/// <summary>
/// A customer. The constructor is private: the only way to get a valid Customer from user input
/// is <see cref="Create"/>, which checks the rules. The repository rebuilds stored customers with
/// <see cref="Rehydrate"/>, which trusts the database.
/// </summary>
public sealed class Customer
{
    public const int NameMaxLength = 200;
    public const int EmailMaxLength = 256;
    public const int PhoneMaxLength = 30;

    private Customer(int id, string name, string email, string? phone, DateOnly customerSince, bool isActive, byte[] rowVersion)
    {
        Id = id;
        Name = name;
        Email = email;
        Phone = phone;
        CustomerSince = customerSince;
        IsActive = isActive;
        RowVersion = rowVersion;
    }

    public int Id { get; }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public string? Phone { get; private set; }

    /// <summary>A calendar date, so DateOnly. No time, no time zone.</summary>
    public DateOnly CustomerSince { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>SQL Server rowversion. Sent back on update so a stale edit is refused instead of overwriting someone else's change.</summary>
    public byte[] RowVersion { get; }

    public static Result<Customer> Create(string? name, string? email, string? phone, DateOnly customerSince, DateOnly today)
    {
        var errors = Validate(name, email, phone, customerSince, today);
        if (errors.Count > 0)
        {
            return Result<Customer>.Failure(errors);
        }

        return new Customer(0, name!.Trim(), email!.Trim().ToLowerInvariant(), NullIfBlank(phone), customerSince, isActive: true, rowVersion: []);
    }

    public static Customer Rehydrate(int id, string name, string email, string? phone, DateOnly customerSince, bool isActive, byte[] rowVersion) =>
        new(id, name, email, phone, customerSince, isActive, rowVersion);

    public Result Update(string? name, string? email, string? phone, DateOnly customerSince, bool isActive, DateOnly today)
    {
        var errors = Validate(name, email, phone, customerSince, today);
        if (errors.Count > 0)
        {
            return Result.Failure(errors);
        }

        Name = name!.Trim();
        Email = email!.Trim().ToLowerInvariant();
        Phone = NullIfBlank(phone);
        CustomerSince = customerSince;
        IsActive = isActive;
        return Result.Success();
    }

    private static List<Error> Validate(string? name, string? email, string? phone, DateOnly customerSince, DateOnly today)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(CustomerErrors.NameRequired);
        }
        else if (name.Trim().Length > NameMaxLength)
        {
            errors.Add(CustomerErrors.NameTooLong);
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(CustomerErrors.EmailRequired);
        }
        else if (email.Trim().Length > EmailMaxLength || !LooksLikeEmail(email.Trim()))
        {
            errors.Add(CustomerErrors.EmailInvalid);
        }

        if (phone is not null && phone.Trim().Length > PhoneMaxLength)
        {
            errors.Add(CustomerErrors.PhoneTooLong);
        }

        if (customerSince > today)
        {
            errors.Add(CustomerErrors.CustomerSinceInFuture);
        }

        return errors;
    }

    // Deliberately loose: one '@' with text either side and a dot in the domain.
    // Real verification is sending an email, not a regex.
    private static bool LooksLikeEmail(string email)
    {
        var at = email.IndexOf('@', StringComparison.Ordinal);
        return at > 0
            && at == email.LastIndexOf('@')
            && email.IndexOf('.', at) > at + 1
            && !email.EndsWith('.')
            && !email.Contains(' ', StringComparison.Ordinal);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
