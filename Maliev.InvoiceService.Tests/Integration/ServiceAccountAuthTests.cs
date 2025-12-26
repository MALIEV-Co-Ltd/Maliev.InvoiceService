using System.Net;
using System.Net.Http.Json;
using Maliev.InvoiceService.Api.Authorization;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using Maliev.InvoiceService.Tests.Testing;

namespace Maliev.InvoiceService.Tests.Integration;

public class ServiceAccountAuthTests : BaseIntegrationTest
{
    public ServiceAccountAuthTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RegisterPdfReference_WithFilesRegisterPermission_ReturnsNoContent()
    {
        // Arrange
        await CleanDatabaseAsync();

        // Create an invoice first using admin client
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            Lines = new List<InvoiceLineItemRequest> { new() { LineNumber = 1, Description = "Item", Quantity = 1, UnitPrice = 100, TaxCategory = "VAT" } }
        });

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            var error = await createResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create invoice: {createResponse.StatusCode}. Error: {error}");
        }

        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Finalize invoice first (PDF can only be registered for finalized invoices)
        var finalizeResponse = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize", new FinalizeInvoiceRequest { FinalizedBy = "test" });
        finalizeResponse.EnsureSuccessStatusCode();

        // Create client with ONLY 'invoice.files.register' permission (simulating UploadService)
        var client = Factory.CreateClient().WithTestAuth(Factory, InvoicePermissions.FilesRegister);

        // Act
        var response = await client.PatchAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/pdf-reference",
            new RegisterPdfFileReferenceRequest { PdfFileReference = "files/invoice-pdf-123.pdf" });

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RegisterPdfReference_WithoutFilesRegisterPermission_ReturnsForbidden()
    {
        // Arrange
        await CleanDatabaseAsync();

        // Create an invoice first
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            Lines = new List<InvoiceLineItemRequest> { new() { LineNumber = 1, Description = "Item", Quantity = 1, UnitPrice = 100, TaxCategory = "VAT" } }
        });

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            var error = await createResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create invoice: {createResponse.StatusCode}. Error: {error}");
        }

        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Create client with 'InvoicesRead' but NOT 'FilesRegister'
        var client = Factory.CreateClient().WithTestAuth(Factory, InvoicePermissions.InvoicesRead);

        // Act
        var response = await client.PatchAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/pdf-reference",
            new RegisterPdfFileReferenceRequest { PdfFileReference = "files/invoice-pdf-123.pdf" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
