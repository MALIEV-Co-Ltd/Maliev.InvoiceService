namespace Maliev.InvoiceService.Api.Models.Common;

/// <summary>
/// Base configuration options for external service HTTP clients.
/// </summary>
public class ExternalServiceOptions
{
    /// <summary>
    /// Gets or sets the base URL of the external service.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout in seconds for HTTP requests.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed requests.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Configuration options for the Currency Service HTTP client.
/// </summary>
public class CurrencyServiceOptions : ExternalServiceOptions
{
}

/// <summary>
/// Configuration options for the Quotation Service HTTP client.
/// </summary>
public class QuotationServiceOptions : ExternalServiceOptions
{
}

/// <summary>
/// Configuration options for the Payment Service HTTP client.
/// </summary>
public class PaymentServiceOptions : ExternalServiceOptions
{
}
