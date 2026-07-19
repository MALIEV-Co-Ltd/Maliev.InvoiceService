using System.Text.Json;
using Maliev.InvoiceService.Application.Services.External;
using Microsoft.Extensions.Logging;

namespace Maliev.InvoiceService.Infrastructure.HttpClients;

/// <summary>
/// HTTP client implementation for retrieving quotation data from the Quotation Service.
/// Implements resilience patterns via Polly (configured in Program.cs).
/// </summary>
public class QuotationServiceClient : IQuotationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<QuotationServiceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuotationServiceClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client configured with base address and resilience policies.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public QuotationServiceClient(HttpClient httpClient, ILogger<QuotationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<QuotationDto?> GetQuotationAsync(string quotationReference, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/quotations/{quotationReference}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Quotation {QuotationReference} not found", quotationReference);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var quotation = JsonSerializer.Deserialize<QuotationDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("Retrieved quotation {QuotationReference} with {LineCount} lines",
                quotationReference, quotation?.Lines.Count ?? 0);

            return quotation;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve quotation {QuotationReference} from quotation service", quotationReference);
            throw new InvalidOperationException($"Quotation service unavailable. Could not retrieve quotation {quotationReference}", ex);
        }
    }
}
