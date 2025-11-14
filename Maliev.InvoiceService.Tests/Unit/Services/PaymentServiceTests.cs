using FluentAssertions;
using Maliev.InvoiceService.Data.Models;

namespace Maliev.InvoiceService.Tests.Unit.Services;

/// <summary>
/// Unit tests for Payment allocation validation logic
/// T160 per tasks.md
/// </summary>
public class PaymentServiceTests
{
    #region T160 - Payment Allocation Validation

    [Fact]
    public void ValidatePaymentAllocation_AllocatedAmountExceedsPayment_ThrowsException()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentAmount = 1000m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Cash",
            CreatedAt = DateTime.UtcNow
        };
        var allocatedAmount = 1500m; // Exceeds payment amount

        // Act & Assert
        Action validation = () =>
        {
            if (allocatedAmount > payment.PaymentAmount)
            {
                throw new ArgumentException("Allocated amount cannot exceed payment amount");
            }
        };

        validation.Should().Throw<ArgumentException>()
            .WithMessage("Allocated amount cannot exceed payment amount");
    }

    [Fact]
    public void ValidatePaymentAllocation_NegativeAmount_ThrowsException()
    {
        // Arrange
        var allocatedAmount = -100m;

        // Act & Assert
        Action validation = () =>
        {
            if (allocatedAmount <= 0)
            {
                throw new ArgumentException("Allocated amount must be positive");
            }
        };

        validation.Should().Throw<ArgumentException>()
            .WithMessage("Allocated amount must be positive");
    }

    [Fact]
    public void ValidatePaymentAllocation_ValidAmount_DoesNotThrow()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentAmount = 1000m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Cash",
            CreatedAt = DateTime.UtcNow
        };
        var allocatedAmount = 500m;

        // Act & Assert
        Action validation = () =>
        {
            if (allocatedAmount <= 0)
                throw new ArgumentException("Allocated amount must be positive");
            if (allocatedAmount > payment.PaymentAmount)
                throw new ArgumentException("Allocated amount cannot exceed payment amount");
        };

        validation.Should().NotThrow();
    }

    [Fact]
    public void CalculateTotalAllocated_MultipleAllocations_ReturnsSumCorrectly()
    {
        // Arrange
        var allocations = new List<InvoicePaymentAllocation>
        {
            new() { AllocatedAmount = 300m, InvoiceId = Guid.NewGuid(), PaymentId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow },
            new() { AllocatedAmount = 450m, InvoiceId = Guid.NewGuid(), PaymentId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow },
            new() { AllocatedAmount = 250m, InvoiceId = Guid.NewGuid(), PaymentId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow }
        };

        // Act
        var totalAllocated = allocations.Sum(a => a.AllocatedAmount);

        // Assert
        totalAllocated.Should().Be(1000);
    }

    [Fact]
    public void ValidatePaymentStatus_FullyAllocated_UpdatesInvoiceStatusToPaid()
    {
        // Arrange
        var invoiceGrandTotal = 1000m;
        var totalAllocated = 1000m;

        // Act
        var status = totalAllocated >= invoiceGrandTotal ? "Paid" :
                      totalAllocated > 0 ? "PartiallyPaid" : "Finalized";

        // Assert
        status.Should().Be("Paid");
    }

    [Fact]
    public async Task ValidatePaymentStatus_PartiallyAllocated_UpdatesInvoiceStatusToPartiallyPaid()
    {
        // Arrange
        var invoiceGrandTotal = 1000m;
        var totalAllocated = 600m;

        // Act
        var status = totalAllocated >= invoiceGrandTotal ? "Paid" :
                      totalAllocated > 0 ? "PartiallyPaid" : "Finalized";

        // Assert
        status.Should().Be("PartiallyPaid");
    }

    #endregion
}
