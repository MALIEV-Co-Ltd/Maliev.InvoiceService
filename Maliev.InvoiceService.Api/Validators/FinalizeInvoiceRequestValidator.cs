using FluentValidation;
using Maliev.InvoiceService.Api.Models.Invoices;

namespace Maliev.InvoiceService.Api.Validators;

/// <summary>
/// Validator for <see cref="FinalizeInvoiceRequest"/>.
/// </summary>
public class FinalizeInvoiceRequestValidator : AbstractValidator<FinalizeInvoiceRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FinalizeInvoiceRequestValidator"/> class.
    /// </summary>
    public FinalizeInvoiceRequestValidator()
    {
        RuleFor(x => x.FinalizedBy)
            .NotEmpty().WithMessage("FinalizedBy is required")
            .MaximumLength(100).WithMessage("FinalizedBy must not exceed 100 characters");
    }
}
