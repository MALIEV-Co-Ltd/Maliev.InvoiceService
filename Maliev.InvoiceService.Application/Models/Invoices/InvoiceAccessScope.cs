namespace Maliev.InvoiceService.Application.Models.Invoices;

/// <summary>
/// Describes caller-specific invoice visibility constraints.
/// </summary>
/// <param name="PrincipalId">Authenticated principal identifier.</param>
/// <param name="RestrictToCreatedInvoices">Whether results must be limited to invoices created by the principal.</param>
public sealed record InvoiceAccessScope(string PrincipalId, bool RestrictToCreatedInvoices)
{
    /// <summary>
    /// Gets a cache-key-safe representation of the scope.
    /// </summary>
    public string CacheKey => RestrictToCreatedInvoices ? $"creator:{PrincipalId}" : "global";
}
