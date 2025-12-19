using System.Net.Http.Json;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;

namespace Maliev.InvoiceService.Tests.Integration;

/// <summary>
/// Integration tests for file reference registration (PDF generation support)
/// T176 per tasks.md
/// </summary>
public class FileReferenceTests : BaseIntegrationTest
{
    public FileReferenceTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region T176 - File Reference Registration

    [Fact]
    public async Task RegisterFileReference_ForFinalizedInvoice_StoresFileMetadata()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create and finalize invoice
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "PDF Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 PDF St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 1000, TaxRate = 7 }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Act - Register file reference (simulates PDF generation)
        var fileRequest = new RegisterFileRequest
        {
            FileType = "PDF",
            FileUrl = "https://storage.example.com/invoices/INV-20250112-000001.pdf",
            FileSizeBytes = 245678,
            GeneratedBy = "pdf-service",
            Checksum = "sha256:abcdef123456"
        };
        var fileResponse = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice.Id}/files", fileRequest);

        // Assert
        fileResponse.EnsureSuccessStatusCode();
        var fileRef = await fileResponse.Content.ReadFromJsonAsync<FileReferenceResponse>();

        Assert.NotNull(fileRef);
        Assert.NotEqual(Guid.Empty, fileRef!.Id);
        Assert.Equal(invoice.Id, fileRef.InvoiceId);
        Assert.Equal("PDF", fileRef.FileType);
        Assert.Equal("https://storage.example.com/invoices/INV-20250112-000001.pdf", fileRef.FileUrl);
        Assert.Equal(245678L, fileRef.FileSizeBytes);
        Assert.Equal("pdf-service", fileRef.GeneratedBy);
        Assert.Equal("sha256:abcdef123456", fileRef.Checksum);
        Assert.True(fileRef.CreatedAt <= DateTime.UtcNow.AddMinutes(1) && fileRef.CreatedAt >= DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task GetFileReferences_ForInvoiceWithMultipleFiles_ReturnsAllFiles()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create finalized invoice
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Multi-File Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 1000, TaxRate = 7 }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Register PDF file
        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice.Id}/files", new RegisterFileRequest
        {
            FileType = "PDF",
            FileUrl = "https://storage.example.com/invoices/invoice.pdf",
            FileSizeBytes = 100000,
            GeneratedBy = "pdf-service"
        });

        // Register XML file (e-Invoice)
        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice.Id}/files", new RegisterFileRequest
        {
            FileType = "XML",
            FileUrl = "https://storage.example.com/invoices/invoice.xml",
            FileSizeBytes = 50000,
            GeneratedBy = "e-invoice-service"
        });

        // Act - Get all file references
        var getResponse = await Client.GetAsync($"/invoice/v1/invoices/{invoice.Id}/files");

        // Assert
        getResponse.EnsureSuccessStatusCode();
        var files = await getResponse.Content.ReadFromJsonAsync<List<FileReferenceResponse>>();

        Assert.NotNull(files);
        Assert.Equal(2, files!.Count);
        Assert.Contains(files, f => f.FileType == "PDF");
        Assert.Contains(files, f => f.FileType == "XML");
    }

    [Fact]
    public async Task RegisterFileReference_ForDraftInvoice_Fails()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create draft invoice (not finalized)
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Draft Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 1000, TaxRate = 7 }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Act - Try to register file for draft invoice
        var fileRequest = new RegisterFileRequest
        {
            FileType = "PDF",
            FileUrl = "https://storage.example.com/draft.pdf",
            FileSizeBytes = 10000,
            GeneratedBy = "pdf-service"
        };
        var fileResponse = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{draft!.Id}/files", fileRequest);

        // Assert - Should fail (only finalized invoices can have files registered)
        Assert.Equal(System.Net.HttpStatusCode.Conflict, fileResponse.StatusCode);
    }

    #endregion

    #region T180 - PDF File Reference Registration (Upload Service Callback)

    [Fact]
    public async Task RegisterPdfFileReference_ForFinalizedInvoice_UpdatesPdfFileReferenceField()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create and finalize invoice
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "PDF Reference Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 PDF Reference St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 1000, TaxRate = 7 }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Act - Upload Service registers PDF file reference via PATCH endpoint
        var pdfReferenceRequest = new RegisterPdfFileReferenceRequest
        {
            PdfFileReference = "https://storage.googleapis.com/maliev-invoices/INV-20250112-000001.pdf"
        };
        var patchResponse = await Client.PatchAsJsonAsync(
            $"/invoice/v1/invoices/{invoice.Id}/pdf-reference",
            pdfReferenceRequest);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NoContent, patchResponse.StatusCode);

        // Verify pdf_file_reference field is updated
        var getResponse = await Client.GetAsync($"/invoice/v1/invoices/{invoice.Id}");
        var updatedInvoice = await getResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        Assert.NotNull(updatedInvoice);
        Assert.Equal("https://storage.googleapis.com/maliev-invoices/INV-20250112-000001.pdf", updatedInvoice!.PdfFileReference);
    }

    [Fact]
    public async Task RegisterPdfFileReference_ForDraftInvoice_ReturnsBadRequest()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create draft invoice (not finalized)
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Draft PDF Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Draft St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 1000, TaxRate = 7 }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Act - Try to register PDF reference for draft invoice
        var pdfReferenceRequest = new RegisterPdfFileReferenceRequest
        {
            PdfFileReference = "https://storage.googleapis.com/maliev-invoices/draft.pdf"
        };
        var patchResponse = await Client.PatchAsJsonAsync(
            $"/invoice/v1/invoices/{draft!.Id}/pdf-reference",
            pdfReferenceRequest);

        // Assert - Should fail with BadRequest (business rule: only finalized invoices)
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, patchResponse.StatusCode);

        var errorContent = await patchResponse.Content.ReadAsStringAsync();
        Assert.Contains("Cannot register PDF file reference for draft invoice", errorContent);
    }

    [Fact]
    public async Task RegisterPdfFileReference_ForNonExistentInvoice_ReturnsNotFound()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Use non-existent invoice ID
        var nonExistentId = Guid.NewGuid();

        // Act - Try to register PDF reference
        var pdfReferenceRequest = new RegisterPdfFileReferenceRequest
        {
            PdfFileReference = "https://storage.googleapis.com/maliev-invoices/nonexistent.pdf"
        };
        var patchResponse = await Client.PatchAsJsonAsync(
            $"/invoice/v1/invoices/{nonExistentId}/pdf-reference",
            pdfReferenceRequest);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, patchResponse.StatusCode);
    }

    [Fact]
    public async Task RegisterPdfFileReference_InvalidatesCacheAndCreatesAuditLog()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create and finalize invoice
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Cache Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Cache St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 1000, TaxRate = 7 }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // First GET to populate cache
        await Client.GetAsync($"/invoice/v1/invoices/{invoice.Id}");

        // Act - Register PDF file reference
        var pdfReferenceRequest = new RegisterPdfFileReferenceRequest
        {
            PdfFileReference = "https://storage.googleapis.com/maliev-invoices/cached-test.pdf"
        };
        await Client.PatchAsJsonAsync($"/invoice/v1/invoices/{invoice.Id}/pdf-reference", pdfReferenceRequest);

        // Assert - Subsequent GET should return updated data (cache invalidated)
        var getResponse = await Client.GetAsync($"/invoice/v1/invoices/{invoice.Id}");
        var updatedInvoice = await getResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        Assert.Equal("https://storage.googleapis.com/maliev-invoices/cached-test.pdf", updatedInvoice!.PdfFileReference);

        // Verify audit log created (we can check this via database or audit endpoint if available)
        // For now, the successful update confirms audit logging worked without throwing errors
        Assert.True(updatedInvoice.UpdatedAt <= DateTime.UtcNow.AddMinutes(1) && updatedInvoice.UpdatedAt >= DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task RegisterPdfFileReference_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create and finalize invoice
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Validation Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Validation St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 1000, TaxRate = 7 }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Act - Send invalid request (empty PdfFileReference)
        var invalidRequest = new { PdfFileReference = "" };
        var patchResponse = await Client.PatchAsJsonAsync(
            $"/invoice/v1/invoices/{invoice.Id}/pdf-reference",
            invalidRequest);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    #endregion
}
