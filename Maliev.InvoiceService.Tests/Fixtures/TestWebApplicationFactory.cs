using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Api.Services.External;
using Maliev.InvoiceService.Tests.Mocks;
using Maliev.InvoiceService.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.InvoiceService.Tests.Fixtures;

public class TestWebApplicationFactory : BaseIntegrationTestFactory<Program, InvoiceDbContext>
{
    protected override void ConfigureAdditionalServices(IServiceCollection services)
    {
        // Replace the real Currency Service client with the mock
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ICurrencyServiceClient));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
        services.AddSingleton<ICurrencyServiceClient, MockCurrencyServiceClient>();

        // Replace the real Quotation Service client with the mock
        var quotationDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IQuotationServiceClient));
        if (quotationDescriptor != null)
        {
            services.Remove(quotationDescriptor);
        }
        services.AddSingleton<IQuotationServiceClient, MockQuotationServiceClient>();
    }
}
