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
    /// Controller for managing invoice files.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
    [Route("invoices/{invoiceId}/file")]
    [ApiController]
    [ApiConventionType(typeof(DefaultApiConventions))]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class FilesController : ControllerBase
    {
        private readonly IInvoiceFileService _invoiceFileService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(IInvoiceFileService invoiceFileService, ILogger<FilesController> logger)
        {
            _invoiceFileService = invoiceFileService;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<InvoiceFileDto>> CreateInvoiceFileAsync(int invoiceId, [FromBody] CreateInvoiceFileRequest request)
        {
            _logger.LogInformation("Attempting to create invoice file for InvoiceId: {InvoiceId}, ObjectName: {ObjectName}", invoiceId, request.ObjectName);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for CreateInvoiceFileAsync.");
                return BadRequest(ModelState);
            }

            var invoiceFileDto = await _invoiceFileService.CreateInvoiceFileAsync(request);

            if (invoiceFileDto == null)
            {
                _logger.LogWarning("Failed to create invoice file for InvoiceId: {InvoiceId}, ObjectName: {ObjectName}. Service returned null.", invoiceId, request.ObjectName);
                return NotFound();
            }

            _logger.LogInformation("Invoice file created successfully with Id: {Id}", invoiceFileDto.Id);
            return CreatedAtRoute("GetInvoiceFile", new { id = invoiceFileDto.Id }, invoiceFileDto);
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteInvoiceFileAsync(int invoiceId)
        {
            _logger.LogInformation("Attempting to delete invoice file for InvoiceId: {InvoiceId}", invoiceId);

            var deleted = await _invoiceFileService.DeleteInvoiceFileAsync(invoiceId);
            if (!deleted)
            {
                _logger.LogWarning("Invoice file for InvoiceId: {InvoiceId} not found for deletion.", invoiceId);
                return NotFound();
            }

            _logger.LogInformation("Invoice file for InvoiceId: {InvoiceId} deleted successfully.", invoiceId);
            return NoContent();
        }

        

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<InvoiceFileDto>> GetInvoiceFileAsync(int invoiceId)
        {
            _logger.LogInformation("Attempting to retrieve invoice file for InvoiceId: {InvoiceId}", invoiceId);

            var invoiceFile = await _invoiceFileService.GetInvoiceFileAsync(invoiceId);
            if (invoiceFile == null)
            {
                _logger.LogWarning("Invoice file for InvoiceId: {InvoiceId} not found.", invoiceId);
                return NotFound();
            }

            _logger.LogInformation("Retrieved invoice file for InvoiceId: {InvoiceId}", invoiceId);
            return Ok(invoiceFile);
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateInvoiceFileAsync(int invoiceId, [FromBody] UpdateInvoiceFileRequest request)
        {
            _logger.LogInformation("Attempting to update invoice file for InvoiceId: {InvoiceId}", invoiceId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for UpdateInvoiceFileAsync for InvoiceId: {InvoiceId}", invoiceId);
                return BadRequest(ModelState);
            }

            var updated = await _invoiceFileService.UpdateInvoiceFileAsync(invoiceId, request);
            if (!updated)
            {
                _logger.LogWarning("Invoice file for InvoiceId: {InvoiceId} not found for update.", invoiceId);
                return NotFound();
            }

            _logger.LogInformation("Invoice file for InvoiceId: {InvoiceId} updated successfully.", invoiceId);
            return NoContent();
        }
    }
}
