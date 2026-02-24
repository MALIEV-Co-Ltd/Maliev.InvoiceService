using Maliev.InvoiceService.Api.Services.External;

namespace Maliev.InvoiceService.Tests.Mocks;

public class MockQuotationServiceClient : IQuotationServiceClient
{
    public Task<QuotationDto?> GetQuotationAsync(string quotationReference, CancellationToken cancellationToken = default)
    {
        // Return null for testing - tests can provide their own test doubles if needed
        return Task.FromResult<QuotationDto?>(null);
    }
}
