namespace Maliev.InvoiceService.Api.Authorization;

/// <summary>
/// Result of checking invoice object access.
/// </summary>
public enum InvoiceAccessDecision
{
    /// <summary>The caller can access the invoice or does not require object scoping.</summary>
    Allowed,

    /// <summary>The invoice does not exist.</summary>
    NotFound,

    /// <summary>The invoice exists but is outside the caller's allowed scope.</summary>
    Forbidden
}
