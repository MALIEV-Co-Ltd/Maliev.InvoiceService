using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Api.Services.External;
using Maliev.InvoiceService.Tests.Mocks;
using Maliev.InvoiceService.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;

namespace Maliev.InvoiceService.Tests.Fixtures;

public class TestWebApplicationFactory : BaseIntegrationTestFactory<Program, InvoiceDbContext>
{
    protected override void ConfigureEnvironmentVariables()
    {
        base.ConfigureEnvironmentVariables();
    }

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

        // Mock IAM registration service calls to prevent fail-fast startup errors
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"status\":\"success\"}")
            });

        services.AddHttpClient("IAMService")
            .ConfigurePrimaryHttpMessageHandler(() => handlerMock.Object);
    }
}
