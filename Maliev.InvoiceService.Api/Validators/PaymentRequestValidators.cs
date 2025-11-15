using FluentValidation;
using Maliev.InvoiceService.Api.Models.Payments;

namespace Maliev.InvoiceService.Api.Validators;

/// <summary>
/// Validator for CreatePaymentRequest, providing validation rules for payment creation.
/// </summary>
public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePaymentRequestValidator"/> class with validation rules.
    /// </summary>
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.PaymentAmount)
            .GreaterThan(0).WithMessage("Payment amount must be greater than 0");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required")
            .MaximumLength(50).WithMessage("Payment method cannot exceed 50 characters");

        RuleFor(x => x.RecordedBy)
            .NotEmpty().WithMessage("RecordedBy is required")
            .MaximumLength(100).WithMessage("RecordedBy cannot exceed 100 characters");

        RuleFor(x => x.PaymentDate)
            .NotEmpty().WithMessage("Payment date is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Payment date cannot be in the future");
    }
}

/// <summary>
/// Validator for LinkPaymentRequest, providing validation rules for linking payments to invoices.
/// </summary>
public class LinkPaymentRequestValidator : AbstractValidator<LinkPaymentRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LinkPaymentRequestValidator"/> class with validation rules.
    /// </summary>
    public LinkPaymentRequestValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty().WithMessage("Payment ID is required");

        RuleFor(x => x.AllocatedAmount)
            .GreaterThan(0).WithMessage("Allocated amount must be greater than 0");
    }
}
