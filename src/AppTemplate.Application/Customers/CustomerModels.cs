namespace AppTemplate.Application.Customers;

/// <summary>A grid row. Read straight from the paging procedure; no entity is built for a list.</summary>
public sealed record CustomerListItem(int CustomerId, string Name, string Email, string? Phone, DateOnly CustomerSince, bool IsActive);

public sealed record CustomerDetails(int CustomerId, string Name, string Email, string? Phone, DateOnly CustomerSince, bool IsActive, byte[] RowVersion);

public sealed record CreateCustomerInput(string? Name, string? Email, string? Phone, DateOnly CustomerSince);

/// <param name="RowVersion">The version the user loaded. If someone saved in between, the update is refused.</param>
public sealed record UpdateCustomerInput(string? Name, string? Email, string? Phone, DateOnly CustomerSince, bool IsActive, byte[] RowVersion);
