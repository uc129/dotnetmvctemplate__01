using AppTemplate.Domain.Common;

namespace AppTemplate.Domain.Customers;

/// <summary>
/// Every error a customer operation can return, in one place, so codes are never typed twice.
/// Field names match the form fields (nameof on the view model would create a dependency on Web).
/// </summary>
public static class CustomerErrors
{
    public static readonly Error NameRequired = Error.Validation("Customer.NameRequired", "Name is required.", "Name");
    public static readonly Error NameTooLong = Error.Validation("Customer.NameTooLong", $"Name must be at most {Customer.NameMaxLength} characters.", "Name");
    public static readonly Error EmailRequired = Error.Validation("Customer.EmailRequired", "Email is required.", "Email");
    public static readonly Error EmailInvalid = Error.Validation("Customer.EmailInvalid", "Email is not a valid address.", "Email");
    public static readonly Error PhoneTooLong = Error.Validation("Customer.PhoneTooLong", $"Phone must be at most {Customer.PhoneMaxLength} characters.", "Phone");
    public static readonly Error CustomerSinceInFuture = Error.Validation("Customer.CustomerSinceInFuture", "Customer since cannot be in the future.", "CustomerSince");
    public static readonly Error NotFound = Error.NotFound("Customer.NotFound", "Customer not found.");
}
