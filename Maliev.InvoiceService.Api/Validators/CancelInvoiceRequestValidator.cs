using FluentValidation;
using Maliev.InvoiceService.Api.Models.Invoices;

namespace Maliev.InvoiceService.Api.Validators;

/// <summary>
/// Validator for <see cref="CancelInvoiceRequest"/>.
/// </summary>
public class CancelInvoiceRequestValidator : AbstractValidator<CancelInvoiceRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelInvoiceRequestValidator"/> class.
    /// </summary>
    public CancelInvoiceRequestValidator()
    {
        RuleFor(x => x.CancelledBy)
            .NotEmpty().WithMessage("CancelledBy is required")
            .MaximumLength(100).WithMessage("CancelledBy cannot exceed 100 characters");

        RuleFor(x => x.CancellationReason)
            .NotEmpty().WithMessage("Cancellation reason is required")
            .MinimumLength(10).WithMessage("Cancellation reason must be at least 10 characters")
            .MaximumLength(500).WithMessage("Cancellation reason cannot exceed 500 characters");
    }
}
