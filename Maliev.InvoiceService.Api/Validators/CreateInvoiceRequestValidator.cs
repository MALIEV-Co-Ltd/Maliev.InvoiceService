using FluentValidation;
using Maliev.InvoiceService.Api.Models.Invoices;

namespace Maliev.InvoiceService.Api.Validators;

/// <summary>
/// Validator for <see cref="CreateInvoiceRequest"/>.
/// </summary>
public class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateInvoiceRequestValidator"/> class.
    /// </summary>
    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required");

        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name is required")
            .MaximumLength(500).WithMessage("Customer name must not exceed 500 characters");

        RuleFor(x => x.CustomerTaxId)
            .NotEmpty().WithMessage("Customer tax ID is required")
            .MaximumLength(50).WithMessage("Customer tax ID must not exceed 50 characters");

        RuleFor(x => x.BillingAddress)
            .NotEmpty().WithMessage("Billing address is required")
            .MaximumLength(2000).WithMessage("Billing address must not exceed 2000 characters");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter ISO 4217 code")
            .Matches("^[A-Z]{3}$").WithMessage("Currency must be uppercase letters");

        RuleFor(x => x.IssueDate)
            .NotEmpty().WithMessage("Issue date is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(30)).WithMessage("Issue date cannot be more than 30 days in the future");

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("Due date is required")
            .GreaterThan(x => x.IssueDate).WithMessage("Due date must be after issue date");

        RuleFor(x => x.PaymentTermsDays)
            .GreaterThan(0).WithMessage("Payment terms days must be positive")
            .LessThanOrEqualTo(365).WithMessage("Payment terms days must not exceed 365");

        RuleFor(x => x.LateFeePercentage)
            .GreaterThanOrEqualTo(0).When(x => x.LateFeePercentage.HasValue)
            .WithMessage("Late fee percentage must be non-negative")
            .LessThanOrEqualTo(100).When(x => x.LateFeePercentage.HasValue)
            .WithMessage("Late fee percentage must not exceed 100");

        RuleFor(x => x.WithholdingTaxPercentage)
            .GreaterThanOrEqualTo(0).WithMessage("Withholding tax percentage must be non-negative")
            .LessThanOrEqualTo(100).WithMessage("Withholding tax percentage must not exceed 100")
            .Must(BeValidWithholdingTaxRate).WithMessage("Withholding tax percentage must be 0, 1, 2, 3, or 5 for Thai regulations");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("At least one line item is required")
            .Must(lines => lines.Count <= 50).WithMessage("Invoice cannot have more than 50 line items");

        RuleForEach(x => x.Lines).SetValidator(new InvoiceLineRequestValidator());
    }

    /// <summary>
    /// Checks if the provided percentage is a valid withholding tax rate according to Thai regulations.
    /// </summary>
    /// <param name="percentage">The withholding tax percentage to validate.</param>
    /// <returns><c>true</c> if the percentage is a valid Thai withholding tax rate; otherwise, <c>false</c>.</returns>
    private static bool BeValidWithholdingTaxRate(decimal percentage)
    {
        // Common Thai withholding tax rates: 0%, 1%, 2%, 3%, 5%
        var validRates = new[] { 0m, 1m, 2m, 3m, 5m };
        return validRates.Contains(percentage);
    }
}
