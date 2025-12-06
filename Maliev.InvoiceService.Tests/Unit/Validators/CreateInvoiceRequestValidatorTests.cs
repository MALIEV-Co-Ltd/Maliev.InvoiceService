using FluentValidation.TestHelper;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Api.Validators;

namespace Maliev.InvoiceService.Tests.Unit.Validators;

/// <summary>
/// Unit tests for CreateInvoiceRequestValidator
/// T073 per tasks.md
/// Tests validation rules in isolation
/// </summary>
public class CreateInvoiceRequestValidatorTests
{
    private readonly CreateInvoiceRequestValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Valid Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Valid St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 100, TaxRate = 7 }
            }
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_MissingCustomerId_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.Empty,
            CustomerName = "Test",
            Currency = "THB",
            Lines = new List<InvoiceLineItemRequest>()
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public async Task Validate_EmptyCustomerName_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "",
            Currency = "THB",
            Lines = new List<InvoiceLineItemRequest>()
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerName);
    }

    [Fact]
    public async Task Validate_InvalidCurrency_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test",
            Currency = "INVALID",
            Lines = new List<InvoiceLineItemRequest>()
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public async Task Validate_DueDateBeforeIssueDate_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(-1), // Due date before issue date
            Lines = new List<InvoiceLineItemRequest>()
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public async Task Validate_EmptyLines_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            Lines = new List<InvoiceLineItemRequest>() // Empty lines
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Lines);
    }

    [Fact]
    public async Task Validate_NegativeWithholdingTax_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test",
            Currency = "THB",
            WithholdingTaxPercentage = -5m, // Negative
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Test", Quantity = 1, UnitPrice = 100, TaxRate = 7 }
            }
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WithholdingTaxPercentage);
    }
}
