using System.Net;
using System.Net.Http.Json;
using Maliev.InvoiceService.Api.Authorization;
using Maliev.InvoiceService.Application.Models.Common;
using Maliev.InvoiceService.Application.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using Maliev.InvoiceService.Tests.Testing;

namespace Maliev.InvoiceService.Tests.Integration;

public class InvoiceCreatorScopeTests : BaseIntegrationTest
{
    public InvoiceCreatorScopeTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreatorRole_CanOnlyReadInvoicesTheyCreated()
    {
        await CleanDatabaseAsync();
        using var creatorA = CreateCreatorClient("creator-a");
        using var creatorB = CreateCreatorClient("creator-b");

        var creatorAInvoiceId = await CreateInvoiceAsync(creatorA);
        var creatorBInvoiceId = await CreateInvoiceAsync(creatorB);

        var forbiddenResponse = await creatorB.GetAsync($"/invoice/v1/invoices/{creatorAInvoiceId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        var allowedResponse = await creatorB.GetAsync($"/invoice/v1/invoices/{creatorBInvoiceId}");
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);

        var invoice = await allowedResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(invoice);
        Assert.Equal("creator-b", invoice!.CreatedBy);
    }

    [Fact]
    public async Task CreatorRole_SearchOnlyReturnsInvoicesTheyCreated()
    {
        await CleanDatabaseAsync();
        using var creatorA = CreateCreatorClient("creator-a");
        using var creatorB = CreateCreatorClient("creator-b");

        await CreateInvoiceAsync(creatorA);
        var creatorBInvoiceId = await CreateInvoiceAsync(creatorB);

        var response = await creatorB.GetAsync("/invoice/v1/invoices?pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<InvoiceResponse>>();
        Assert.NotNull(result);
        var invoice = Assert.Single(result!.Items);
        Assert.Equal(creatorBInvoiceId, invoice.Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task CreatorRole_CannotUpdateInvoicesCreatedByAnotherPrincipal()
    {
        await CleanDatabaseAsync();
        using var creatorA = CreateCreatorClient("creator-a");
        using var creatorB = CreateCreatorClient("creator-b");

        var creatorAInvoiceId = await CreateInvoiceAsync(creatorA);

        var response = await creatorB.PutAsJsonAsync(
            $"/invoice/v1/invoices/{creatorAInvoiceId}",
            CreateUpdateRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient CreateCreatorClient(string userId)
    {
        return Factory.CreateClient().WithTestAuth(
            Factory,
            userId,
            [InvoicePredefinedRoles.Creator],
            CreatorPermissions());
    }

    private static string[] CreatorPermissions()
    {
        return InvoicePredefinedRoles.All
            .Single(role => role.RoleId == InvoicePredefinedRoles.Creator)
            .Permissions;
    }

    private static async Task<Guid> CreateInvoiceAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/invoice/v1/invoices", CreateInvoiceRequest());
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Invoice creation failed: {response.StatusCode} - {error}");
        }

        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(invoice);
        return invoice!.Id;
    }

    private static CreateInvoiceRequest CreateInvoiceRequest()
    {
        return new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St, Bangkok",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines =
            [
                new InvoiceLineItemRequest
                {
                    LineNumber = 1,
                    Description = "Test Product",
                    Quantity = 1,
                    UnitPrice = 100,
                    TaxRate = 7
                }
            ]
        };
    }

    private static UpdateInvoiceRequest CreateUpdateRequest()
    {
        return new UpdateInvoiceRequest
        {
            CustomerName = "Updated Customer",
            CustomerTaxId = "9876543210987",
            BillingAddress = "456 New St, Bangkok",
            DueDate = DateTime.UtcNow.Date.AddDays(45),
            PaymentTermsDays = 45,
            Lines =
            [
                new InvoiceLineItemRequest
                {
                    LineNumber = 1,
                    Description = "Updated Product",
                    Quantity = 2,
                    UnitPrice = 125,
                    TaxRate = 7
                }
            ]
        };
    }
}
