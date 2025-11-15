using FluentValidation;
using Maliev.InvoiceService.Api.Models.Invoices;

namespace Maliev.InvoiceService.Api.Validators;

/// <summary>
/// Validator for <see cref="InvoiceLineItemRequest"/>.
/// </summary>
public class InvoiceLineRequestValidator : AbstractValidator<InvoiceLineItemRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceLineRequestValidator"/> class.
    /// </summary>
    public InvoiceLineRequestValidator()
    {
        RuleFor(x => x.LineNumber)
            .GreaterThan(0).WithMessage("Line number must be positive");

        RuleFor(x => x.ItemCode)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.ItemCode))
            .WithMessage("Item code must not exceed 100 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be positive");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price must be non-negative");

        RuleFor(x => x.DiscountPercentage)
            .GreaterThanOrEqualTo(0).WithMessage("Discount percentage must be non-negative")
            .LessThanOrEqualTo(100).WithMessage("Discount percentage must not exceed 100");

        RuleFor(x => x.TaxCategory)
            .NotEmpty().WithMessage("Tax category is required")
            .MaximumLength(50).WithMessage("Tax category must not exceed 50 characters");

        RuleFor(x => x.TaxRate)
            .GreaterThanOrEqualTo(0).WithMessage("Tax rate must be non-negative")
            .LessThanOrEqualTo(100).WithMessage("Tax rate must not exceed 100");
    }
}
