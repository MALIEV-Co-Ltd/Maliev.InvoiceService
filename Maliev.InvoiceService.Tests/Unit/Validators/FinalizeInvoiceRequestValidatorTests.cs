using FluentValidation.TestHelper;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Api.Validators;

namespace Maliev.InvoiceService.Tests.Unit.Validators;

/// <summary>
/// Unit tests for FinalizeInvoiceRequestValidator
/// T074 per tasks.md
/// </summary>
public class FinalizeInvoiceRequestValidatorTests
{
    private readonly FinalizeInvoiceRequestValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new FinalizeInvoiceRequest
        {
            FinalizedBy = "valid-user"
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_EmptyFinalizedBy_ShouldHaveValidationError()
    {
        // Arrange
        var request = new FinalizeInvoiceRequest
        {
            FinalizedBy = ""
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FinalizedBy);
    }

    [Fact]
    public async Task Validate_NullFinalizedBy_ShouldHaveValidationError()
    {
        // Arrange
        var request = new FinalizeInvoiceRequest
        {
            FinalizedBy = null!
        };

        // Act
        var result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FinalizedBy);
    }
}
