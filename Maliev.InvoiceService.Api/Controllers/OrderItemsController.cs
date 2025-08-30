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
    /// Controller for managing order items.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
    [Route("invoices/{invoiceId}/order-items")]
    [ApiController]
    [ApiConventionType(typeof(DefaultApiConventions))]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class OrderItemsController : ControllerBase
    {
        private readonly IOrderItemService _orderItemService;
        private readonly ILogger<OrderItemsController> _logger;

        public OrderItemsController(IOrderItemService orderItemService, ILogger<OrderItemsController> logger)
        {
            _orderItemService = orderItemService;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<OrderItemDto>> CreateOrderItemAsync([FromBody] CreateOrderItemRequest request)
        {
            _logger.LogInformation("Attempting to create order item for InvoiceId: {InvoiceId}, Description: {Description}", request.InvoiceId, request.Description);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for CreateOrderItemAsync.");
                return BadRequest(ModelState);
            }

            var orderItemDto = await _orderItemService.CreateOrderItemAsync(request);

            _logger.LogInformation("Order item created successfully with Id: {Id}", orderItemDto.Id);
            return CreatedAtRoute("GetOrderItem", new { id = orderItemDto.Id }, orderItemDto);
        }

        [HttpDelete("{orderItemId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteOrderItemAsync(int invoiceId, int orderItemId)
        {
            _logger.LogInformation("Attempting to delete order item with Id: {OrderItemId} for InvoiceId: {InvoiceId}", orderItemId, invoiceId);

            var deleted = await _orderItemService.DeleteOrderItemAsync(orderItemId);
            if (!deleted)
            {
                _logger.LogWarning("Order item with Id: {OrderItemId} for InvoiceId: {InvoiceId} not found for deletion.", orderItemId, invoiceId);
                return NotFound();
            }

            _logger.LogInformation("Order item with Id: {OrderItemId} for InvoiceId: {InvoiceId} deleted successfully.", orderItemId, invoiceId);
            return NoContent();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<OrderItemDto>>> GetOrderItemsAsync(int invoiceId)
        {
            _logger.LogInformation("Attempting to retrieve order items for InvoiceId: {InvoiceId}", invoiceId);

            var orderItems = await _orderItemService.GetOrderItemsAsync(invoiceId);
            if (orderItems == null || orderItems.Count == 0)
            {
                _logger.LogWarning("No order items found for InvoiceId: {InvoiceId}.", invoiceId);
                return NotFound();
            }

            _logger.LogInformation("Retrieved {Count} order items for InvoiceId: {InvoiceId}.", orderItems.Count, invoiceId);
            return Ok(orderItems);
        }

        [HttpGet("{orderItemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderItemDto>> GetOrderItemAsync(int invoiceId, int orderItemId)
        {
            _logger.LogInformation("Attempting to retrieve order item with Id: {OrderItemId} for InvoiceId: {InvoiceId}", orderItemId, invoiceId);

            var orderItem = await _orderItemService.GetOrderItemAsync(orderItemId);
            if (orderItem == null)
            {
                _logger.LogWarning("Order item with Id: {OrderItemId} for InvoiceId: {InvoiceId} not found.", orderItemId, invoiceId);
                return NotFound();
            }

            _logger.LogInformation("Retrieved order item with Id: {OrderItemId} for InvoiceId: {InvoiceId}", orderItemId, invoiceId);
            return Ok(orderItem);
        }

        [HttpPut("{orderItemId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateOrderItemAsync(int invoiceId, int orderItemId, [FromBody] UpdateOrderItemRequest request)
        {
            _logger.LogInformation("Attempting to update order item with Id: {OrderItemId} for InvoiceId: {InvoiceId}", orderItemId, invoiceId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for UpdateOrderItemAsync for OrderItemId: {OrderItemId} and InvoiceId: {InvoiceId}", orderItemId, invoiceId);
                return BadRequest(ModelState);
            }

            var updated = await _orderItemService.UpdateOrderItemAsync(orderItemId, request);
            if (!updated)
            {
                _logger.LogWarning("Order item with Id: {OrderItemId} for InvoiceId: {InvoiceId} not found for update.", orderItemId, invoiceId);
                return NotFound();
            }

            _logger.LogInformation("Order item with Id: {OrderItemId} for InvoiceId: {InvoiceId} updated successfully.", orderItemId, invoiceId);
            return NoContent();
        }
    }
}