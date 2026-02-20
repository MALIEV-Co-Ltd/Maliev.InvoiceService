namespace Maliev.InvoiceService.Api.Models.Customers;

/// <summary>
/// Customer response DTO from Customer Service API (subset of fields for billing identity validation)
/// </summary>
public class CustomerResponse
{
    /// <summary>
    /// Customer unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Customer first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Customer last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Masked Thai National ID (last 2 digits only, for security)
    /// </summary>
    public string? ThaiNationalIdMasked { get; set; }

    /// <summary>
    /// Linked company identifier (if customer belongs to a company)
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Linked company name (if customer belongs to a company)
    /// </summary>
    public string? CompanyName { get; set; }
}
