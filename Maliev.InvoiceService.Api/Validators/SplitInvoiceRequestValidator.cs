using FluentValidation;
using Maliev.InvoiceService.Api.Models.Invoices;

namespace Maliev.InvoiceService.Api.Validators;

/// <summary>
/// Validator for <see cref="SplitInvoiceRequest"/>.
/// </summary>
public class SplitInvoiceRequestValidator : AbstractValidator<SplitInvoiceRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SplitInvoiceRequestValidator"/> class.
    /// </summary>
    public SplitInvoiceRequestValidator()
    {
        RuleFor(x => x.SplitRules)
            .NotEmpty().WithMessage("At least one split rule is required")
            .Must(rules => rules != null && rules.Count >= 2)
            .WithMessage("At least 2 split rules are required");

        RuleFor(x => x.SplitRules)
            .Must(rules => rules != null && Math.Abs(rules.Sum(r => r.Percentage) - 100) < 0.01m)
            .WithMessage("Split percentages must sum to 100%")
            .When(x => x.SplitRules != null && x.SplitRules.Any());

        RuleForEach(x => x.SplitRules)
            .ChildRules(rule =>
            {
                rule.RuleFor(r => r.Percentage)
                    .GreaterThan(0).WithMessage("Percentage must be greater than 0")
                    .LessThanOrEqualTo(100).WithMessage("Percentage cannot exceed 100");
            });
    }
}

/// <summary>
/// Validator for <see cref="InvoiceSplitRule"/>.
/// </summary>
public class InvoiceSplitRuleValidator : AbstractValidator<InvoiceSplitRule>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceSplitRuleValidator"/> class.
    /// </summary>
    public InvoiceSplitRuleValidator()
    {
        RuleFor(x => x.Percentage)
            .GreaterThan(0).WithMessage("Percentage must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Percentage cannot exceed 100");
    }
}
