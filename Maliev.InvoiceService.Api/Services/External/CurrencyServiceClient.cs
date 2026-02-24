using System.Text.Json;

namespace Maliev.InvoiceService.Api.Services.External;

/// <summary>
/// HTTP client implementation for retrieving exchange rates from the Currency Service.
/// Implements resilience patterns via Polly (configured in Program.cs).
/// </summary>
public class CurrencyServiceClient : ICurrencyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CurrencyServiceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencyServiceClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client configured with base address and resilience policies.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public CurrencyServiceClient(HttpClient httpClient, ILogger<CurrencyServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<decimal> GetExchangeRateAsync(
        string fromCurrency,
        string toCurrency,
        DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dateParam = date?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync(
                $"/api/v1/exchange-rates?from={fromCurrency}&to={toCurrency}&date={dateParam}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var exchangeRateResponse = JsonSerializer.Deserialize<ExchangeRateResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (exchangeRateResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize exchange rate response");
            }

            _logger.LogInformation("Retrieved exchange rate {From} -> {To}: {Rate} from {Source}",
                fromCurrency, toCurrency, exchangeRateResponse.Rate, exchangeRateResponse.Source);

            return exchangeRateResponse.Rate;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve exchange rate from currency service for {From} -> {To}",
                fromCurrency, toCurrency);
            throw new InvalidOperationException($"Currency service unavailable. Could not retrieve exchange rate for {fromCurrency} -> {toCurrency}", ex);
        }
    }
}
