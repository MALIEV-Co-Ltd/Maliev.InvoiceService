using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.InvoiceService.Api.Authorization;
using Maliev.InvoiceService.Infrastructure.Persistence;
using Maliev.InvoiceService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.InvoiceService.Api.Controllers;

/// <summary>
/// Controller for managing credit terms reference data.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("invoice/v{version:apiVersion}/credit-terms")]
public class CreditTermsController : ControllerBase
{
    private readonly InvoiceDbContext _context;
    private readonly ILogger<CreditTermsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreditTermsController"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="logger">Logger instance.</param>
    public CreditTermsController(InvoiceDbContext context, ILogger<CreditTermsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all active credit terms.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of credit terms.</returns>
    [HttpGet]
    [RequirePermission(InvoicePermissions.CreditTermsRead)]
    [ProducesResponseType(typeof(List<CreditTerm>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CreditTerm>>> GetCreditTerms(CancellationToken cancellationToken)
    {
        var terms = await _context.CreditTerms
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Days)
            .ToListAsync(cancellationToken);

        return Ok(terms);
    }

    /// <summary>
    /// Retrieves a credit term by code.
    /// </summary>
    /// <param name="code">Credit term code (e.g., NET30).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Credit term details.</returns>
    [HttpGet("{code}")]
    [RequirePermission(InvoicePermissions.CreditTermsRead)]
    [ProducesResponseType(typeof(CreditTerm), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditTerm>> GetCreditTerm(string code, CancellationToken cancellationToken)
    {
        var term = await _context.CreditTerms
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == code, cancellationToken);

        if (term == null)
            return NotFound();

        return Ok(term);
    }
}
