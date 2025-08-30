using Maliev.InvoiceService.Api.Models;
using Maliev.InvoiceService.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maliev.InvoiceService.Api.Controllers
{
    /// <summary>
    /// Controller.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
    [Route("api/invoices")]
    [ApiController]
    [ApiConventionType(typeof(DefaultApiConventions))]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(IInvoiceService invoiceService, ILogger<InvoicesController> logger)
        {
            _invoiceService = invoiceService;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<InvoiceDto>> CreateInvoiceAsync([FromBody] CreateInvoiceRequest request)
        {
            _logger.LogInformation("Attempting to create invoice with Number: {Number}", request.Number);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for CreateInvoiceAsync.");
                return BadRequest(ModelState);
            }

            var invoiceDto = await _invoiceService.CreateInvoiceAsync(request);

            _logger.LogInformation("Invoice created successfully with Id: {Id}", invoiceDto.Id);
            return CreatedAtRoute("GetInvoice", new { id = invoiceDto.Id }, invoiceDto);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteInvoiceAsync(int id)
        {
            _logger.LogInformation("Attempting to delete invoice with Id: {Id}", id);

            var deleted = await _invoiceService.DeleteInvoiceAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("Invoice with Id: {Id} not found for deletion.", id);
                return NotFound();
            }

            _logger.LogInformation("Invoice with Id: {Id} deleted successfully.", id);
            return NoContent();
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PaginatedResponse<InvoiceDto>>> GetAllInvoicesAsync([FromQuery] InvoicePaginationRequest request)
        {
            _logger.LogInformation("Attempting to retrieve all invoices with PageNumber: {PageNumber}, PageSize: {PageSize}", request.PageNumber, request.PageSize);

            var invoices = await _invoiceService.GetAllInvoicesAsync(request);
            if (invoices == null || invoices.Items.Count == 0)
            {
                _logger.LogWarning("No invoices found.");
                return NotFound();
            }

            _logger.LogInformation("Retrieved {Count} invoices.", invoices.Items.Count);
            return Ok(invoices);
        }

        [HttpGet("{id}", Name = "GetInvoiceById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<InvoiceDto>> GetInvoiceByIdAsync(int id)
        {
            _logger.LogInformation("Attempting to retrieve invoice with Id: {Id}", id);

            var invoiceDto = await _invoiceService.GetInvoiceAsync(id);
            if (invoiceDto == null)
            {
                _logger.LogWarning("Invoice with Id: {Id} not found.", id);
                return NotFound();
            }

            _logger.LogInformation("Retrieved invoice with Id: {Id}", invoiceDto.Id);
            return Ok(invoiceDto);
        }

        [HttpGet("by-number/{number}", Name = "GetInvoiceByNumber")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<InvoiceDto>> GetInvoiceByNumberAsync(string number)
        {
            _logger.LogInformation("Attempting to retrieve invoice with Number: {Number}", number);

            var invoiceDto = await _invoiceService.GetInvoiceByNumberAsync(number);
            if (invoiceDto == null)
            {
                _logger.LogWarning("Invoice with Number: {Number} not found.", number);
                return NotFound();
            }

            _logger.LogInformation("Retrieved invoice with Id: {Id}", invoiceDto.Id);
            return Ok(invoiceDto);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateInvoiceAsync(int id, [FromBody] UpdateInvoiceRequest request)
        {
            _logger.LogInformation("Attempting to update invoice with Id: {Id}", id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for UpdateInvoiceAsync for Id: {Id}", id);
                return BadRequest(ModelState);
            }

            var updated = await _invoiceService.UpdateInvoiceAsync(id, request);
            if (!updated)
            {
                _logger.LogWarning("Invoice with Id: {Id} not found for update.", id);
                return NotFound();
            }

            _logger.LogInformation("Invoice with Id: {Id} updated successfully.", id);
            return NoContent();
        }
    }
}