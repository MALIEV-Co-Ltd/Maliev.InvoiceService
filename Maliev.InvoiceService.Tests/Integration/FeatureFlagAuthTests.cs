using System.Net;
using System.Net.Http.Json;
using Maliev.InvoiceService.Api.Authorization;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using Maliev.InvoiceService.Tests.Testing;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Maliev.InvoiceService.Tests.Integration;

public class FeatureFlagAuthTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public FeatureFlagAuthTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WhenFeatureFlagDisabled_ShouldAllowBasedOnLegacyRoles()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:PermissionBasedAuthEnabled"] = "false"
                });
            });
        }).CreateClient();

        // Create client with ONLY 'Manager' role claim
        var claims = new Dictionary<string, string>
        {
            [ClaimTypes.Role] = "Manager"
        };
        var token = _factory.CreateTestJwtToken("test-user", null, claims);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        // Act
        // Accessing GetInvoice which traditionally might be [AllowAnonymous] or broad
        // Let's use Finalize which was [Authorize(Policy = "Manager")]
        var response = await client.PostAsJsonAsync($"/invoice/v1/invoices/{Guid.NewGuid()}/finalize", new FinalizeInvoiceRequest { FinalizedBy = "test" });

        // Assert
        // Should NOT be Forbidden because feature flag is OFF, so it falls back to legacy roles
        // It might be NotFound if invoice doesn't exist, but NOT Forbidden
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WhenFeatureFlagEnabled_ShouldDenyIfPermissionsMissing()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:PermissionBasedAuthEnabled"] = "true"
                });
            });
        }).CreateClient();

        // Create client with ONLY an unmapped role claim (NO permissions)
        var claims = new Dictionary<string, string>
        {
            [ClaimTypes.Role] = "UnmappedRole"
        };
        var token = _factory.CreateTestJwtToken("test-user", null, claims);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        // Act
        var response = await client.PostAsJsonAsync($"/invoice/v1/invoices/{Guid.NewGuid()}/finalize", new FinalizeInvoiceRequest { FinalizedBy = "test" });

        // Assert
        // Should be Forbidden because feature flag is ON and no permissions are present
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
