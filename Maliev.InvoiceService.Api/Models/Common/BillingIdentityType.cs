namespace Maliev.InvoiceService.Api.Models.Common;

/// <summary>
/// Specifies which identity to use for billing on invoices
/// </summary>
public enum BillingIdentityType
{
    /// <summary>
    /// Use customer's personal identity (Thai National ID)
    /// </summary>
    Personal = 0,

    /// <summary>
    /// Use customer's linked company identity (Company Tax ID / VAT Number)
    /// </summary>
    Corporate = 1
}
