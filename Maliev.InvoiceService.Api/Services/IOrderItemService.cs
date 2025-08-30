using Maliev.InvoiceService.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maliev.InvoiceService.Api.Services
{
    public interface IOrderItemService
    {
        Task<OrderItemDto> CreateOrderItemAsync(CreateOrderItemRequest request);
        Task<bool> DeleteOrderItemAsync(int id);
        Task<List<OrderItemDto>> GetAllOrderItemsAsync();
        Task<List<OrderItemDto>> GetOrderItemsAsync(int invoiceId);
        Task<OrderItemDto?> GetOrderItemAsync(int id);
        Task<bool> UpdateOrderItemAsync(int id, UpdateOrderItemRequest request);
    }
}
