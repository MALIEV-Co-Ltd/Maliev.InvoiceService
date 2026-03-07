using System.Net;
using System.Text.Json;
using Maliev.InvoiceService.Application.Services.External;
using Microsoft.Extensions.Logging;

namespace Maliev.InvoiceService.Infrastructure.HttpClients;

/// <summary>
/// HTTP client for interacting with the Payment Service API.
/// Implements resilience patterns via Polly (configured in Program.cs).
/// </summary>
public class PaymentServiceClient : IPaymentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentServiceClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentServiceClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client configured with base address and resilience policies.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public PaymentServiceClient(HttpClient httpClient, ILogger<PaymentServiceClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc/>
    public async Task<ExternalPaymentResponse?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching payment details from Payment Service: PaymentId={PaymentId}", paymentId);

        try
        {
            var response = await _httpClient.GetAsync($"/payments/v1/payments/{paymentId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Payment not found in Payment Service: PaymentId={PaymentId}", paymentId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var payment = JsonSerializer.Deserialize<ExternalPaymentResponse>(content, _jsonOptions);

            _logger.LogInformation(
                "Successfully retrieved payment: PaymentId={PaymentId}, Status={Status}, Amount={Amount} {Currency}",
                paymentId, payment?.Status, payment?.Amount, payment?.Currency);

            return payment;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching payment from Payment Service: PaymentId={PaymentId}", paymentId);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for payment response: PaymentId={PaymentId}", paymentId);
            throw new InvalidOperationException($"Invalid payment response format for PaymentId={paymentId}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidatePaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating payment in Payment Service: PaymentId={PaymentId}", paymentId);

        try
        {
            var payment = await GetPaymentAsync(paymentId, cancellationToken);

            if (payment == null)
            {
                _logger.LogWarning("Payment validation failed: Payment not found. PaymentId={PaymentId}", paymentId);
                return false;
            }

            var isValid = string.Equals(payment.Status, "Succeeded", StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                _logger.LogWarning(
                    "Payment validation failed: Status is not 'Succeeded'. PaymentId={PaymentId}, ActualStatus={Status}",
                    paymentId, payment.Status);
            }
            else
            {
                _logger.LogInformation(
                    "Payment validation successful: PaymentId={PaymentId}, Amount={Amount} {Currency}",
                    paymentId, payment.Amount, payment.Currency);
            }

            return isValid;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Payment validation failed due to HTTP error: PaymentId={PaymentId}", paymentId);
            throw;
        }
    }
}
