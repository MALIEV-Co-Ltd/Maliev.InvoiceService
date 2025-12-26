using System.Net;
using System.Net.Http.Json;
using Maliev.InvoiceService.Api.Authorization;
using Maliev.InvoiceService.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;

namespace Maliev.InvoiceService.Tests.Integration;

public class IAMRegistrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public IAMRegistrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Startup_ShouldRegisterPermissionsAndRolesWithIAM()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        // Setup expected responses for permissions registration
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.AbsolutePath.Contains("/iam/v1/permissions/register")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new { message = "Success" })
            });

        // Setup expected responses for roles registration
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.AbsolutePath.Contains("/iam/v1/roles/register")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new { message = "Success" })
            });

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace the IAM HTTP client with one using our mock handler
                services.AddHttpClient("IAMService")
                    .ConfigurePrimaryHttpMessageHandler(() => handlerMock.Object);
            });
        }).CreateClient();

        // Act - Accessing the client triggers host startup and the registration service
        await client.GetAsync("/invoice/v1/invoices");

        // Assert
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri != null &&
                req.RequestUri.AbsolutePath.Contains("/iam/v1/permissions/register")),
            ItExpr.IsAny<CancellationToken>()
        );

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri != null &&
                req.RequestUri.AbsolutePath.Contains("/iam/v1/roles/register")),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}
