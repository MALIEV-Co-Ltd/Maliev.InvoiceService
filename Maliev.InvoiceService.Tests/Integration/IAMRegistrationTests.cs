using System.Net;
using Maliev.InvoiceService.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MassTransit.Testing;
using Maliev.MessagingContracts.Generated;
using Maliev.MessagingContracts.Contracts.Iam;

namespace Maliev.InvoiceService.Tests.Integration;

[Collection("InvoiceService Collection")]
public class IAMRegistrationTests
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
        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            var client = _factory.CreateClient();

            // Act - Accessing the client triggers host startup and the registration service
            await client.GetAsync("/invoice/v1/invoices");

            // Assert - Verify PermissionRegistrationRequest was published
            // BackgroundIAMRegistrationService waits for app start, then publishes
            Assert.True(await harness.Published.Any<PermissionRegistrationRequest>(x =>
                x.Context.Message.ServiceName == "invoice"),
                "PermissionRegistrationRequest should be published to RabbitMQ");

            var publishedMessage = harness.Published.Select<PermissionRegistrationRequest>()
                .FirstOrDefault(x => x.Context.Message.ServiceName == "invoice");

            Assert.NotNull(publishedMessage);
            var request = publishedMessage.Context.Message;

            // Verify content
            Assert.NotEmpty(request.Permissions);
            Assert.NotEmpty(request.Roles);
            Assert.Contains(request.Permissions, p => p.PermissionId == "invoice.invoices.create");
            Assert.Contains(request.Roles, r => r.RoleId == "roles.invoice.admin");
        }
        finally
        {
            await harness.Stop();
        }
    }
}
