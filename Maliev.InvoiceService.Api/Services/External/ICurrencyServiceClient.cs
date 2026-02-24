namespace Maliev.InvoiceService.Api.Services.External;

/// <summary>
/// Client interface for retrieving exchange rates from the Currency Service.
/// </summary>
public interface ICurrencyServiceClient
{
    /// <summary>
    /// Retrieves the exchange rate between two currencies for a specific date.
    /// </summary>
    /// <param name="fromCurrency">Source currency code (ISO 4217, e.g., "USD").</param>
    /// <param name="toCurrency">Target currency code (ISO 4217, e.g., "THB").</param>
    /// <param name="date">Optional date for historical rates. If null, uses current rate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exchange rate from source to target currency.</returns>
    /// <exception cref="HttpRequestException">When Currency Service is unavailable or returns an error.</exception>
    Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency, DateTime? date = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Response DTO from Currency Service containing exchange rate information.
/// </summary>
public class ExchangeRateResponse
{
    /// <summary>
    /// Gets or sets the source currency code (ISO 4217).
    /// </summary>
    public string FromCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target currency code (ISO 4217).
    /// </summary>
    public string ToCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exchange rate from source to target currency.
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>
    /// Gets or sets the date for which the exchange rate is valid.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the source of the exchange rate data (e.g., "ECB", "OpenExchangeRates").
    /// </summary>
    public string Source { get; set; } = string.Empty;
}
