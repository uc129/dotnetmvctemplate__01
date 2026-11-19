using System.ComponentModel.DataAnnotations;
using AppTemplate.Application.Customers;
using AppTemplate.Domain.Customers;

namespace AppTemplate.Web.Models.Customers;

/// <summary>
/// The create/edit form. The attributes drive client-side validation for a quick response;
/// the Customer entity is the real rule, and its errors come back through Result.
/// Lengths reuse the entity's constants so the two cannot drift.
/// </summary>
public sealed class CustomerFormModel
{
    [Required]
    [StringLength(Customer.NameMaxLength)]
    public string? Name { get; set; }

    [Required]
    [StringLength(Customer.EmailMaxLength)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(Customer.PhoneMaxLength)]
    public string? Phone { get; set; }

    [Display(Name = "Customer since")]
    public DateOnly CustomerSince { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>Round-trips as base64 in a hidden field (MVC's byte[] binder handles it).</summary>
    public byte[]? RowVersion { get; set; }

    public static CustomerFormModel From(CustomerDetails c) => new()
    {
        Name = c.Name,
        Email = c.Email,
        Phone = c.Phone,
        CustomerSince = c.CustomerSince,
        IsActive = c.IsActive,
        RowVersion = c.RowVersion,
    };

    public CreateCustomerInput ToCreateInput() => new(Name, Email, Phone, CustomerSince);

    public UpdateCustomerInput ToUpdateInput() => new(Name, Email, Phone, CustomerSince, IsActive, RowVersion ?? []);
}
