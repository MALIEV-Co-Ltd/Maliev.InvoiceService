using Maliev.InvoiceService.Api.Services.External;

namespace Maliev.InvoiceService.Tests.Mocks;

public class MockCurrencyServiceClient : ICurrencyServiceClient
{
    public Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        // Return mock exchange rates for testing
        var rates = new Dictionary<string, decimal>
        {
            { "USD-THB", 35.00m },
            { "EUR-THB", 38.50m },
            { "JPY-THB", 0.25m },
            { "SGD-THB", 26.00m }
        };

        var key = $"{fromCurrency}-{toCurrency}";
        return Task.FromResult(rates.GetValueOrDefault(key, 1.0m));
    }
}
